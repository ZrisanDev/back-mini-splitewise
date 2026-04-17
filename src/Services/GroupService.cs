using Microsoft.EntityFrameworkCore;
using back_api_splitwise.src.Data;
using back_api_splitwise.src.DTOs.Groups;
using back_api_splitwise.src.DTOs.Pagination;
using back_api_splitwise.src.Entities;
using back_api_splitwise.src.Extensions;
using back_api_splitwise.src.Services.Interfaces;

namespace back_api_splitwise.src.Services;

public class GroupService : IGroupService
{
    private readonly AppDbContext _db;

    public GroupService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GroupResponse> CreateAsync(string name, Guid createdById)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedBy = createdById,
            CreatedAt = DateTime.UtcNow
        };

        var groupUser = new GroupUser
        {
            Id = Guid.NewGuid(),
            UserId = createdById,
            GroupId = group.Id,
            Role = "Admin",
            JoinedAt = DateTime.UtcNow,
            InvitedBy = null
        };

        _db.Groups.Add(group);
        _db.GroupUsers.Add(groupUser);
        await _db.SaveChangesAsync();

        return await MapToGroupResponseAsync(group);
    }

    public async Task<PagedResponse<GroupResponse>> GetByUserAsync(Guid userId, int page, int pageSize)
    {
        var query = _db.GroupUsers
            .Where(gu => gu.UserId == userId)
            .Include(gu => gu.Group)
            .Select(gu => gu.Group!);

        var totalCount = await query.CountAsync();
        var groups = await query
            .OrderByDescending(g => g.CreatedAt)
            .Paginate(page, pageSize)
            .ToListAsync();

        var responses = new List<GroupResponse>();
        foreach (var group in groups)
        {
            responses.Add(await MapToGroupResponseAsync(group));
        }

        return responses.ToPagedResponse(page, pageSize, totalCount);
    }

    public async Task<GroupResponse?> GetByIdAsync(Guid id, Guid userId)
    {
        var isMember = await _db.GroupUsers
            .AnyAsync(gu => gu.GroupId == id && gu.UserId == userId);

        if (!isMember)
            throw new UnauthorizedAccessException("No sos miembro de este grupo.");

        var group = await _db.Groups
            .Include(g => g.GroupUsers)
                .ThenInclude(gu => gu.User)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group is null)
            return null;

        return MapToGroupResponse(group);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var isAdmin = await IsGroupAdminAsync(userId, id);
        if (!isAdmin)
            throw new UnauthorizedAccessException("Solo un Admin puede eliminar el grupo.");

        var group = await _db.Groups.FindAsync(id)
            ?? throw new KeyNotFoundException("Grupo no encontrado.");

        group.IsDeleted = true;
        group.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<GroupUser> AddUserAsync(Guid groupId, Guid userIdToAdd, string role, Guid addedById)
    {
        var groupExists = await _db.Groups.AnyAsync(g => g.Id == groupId);
        if (!groupExists)
            throw new KeyNotFoundException("Grupo no encontrado.");

        var alreadyMember = await _db.GroupUsers
            .AnyAsync(gu => gu.GroupId == groupId && gu.UserId == userIdToAdd);
        if (alreadyMember)
            throw new InvalidOperationException("El usuario ya es miembro del grupo.");

        var userExists = await _db.Users.AnyAsync(u => u.Id == userIdToAdd && u.IsActive);
        if (!userExists)
            throw new KeyNotFoundException("Usuario no encontrado.");

        var groupUser = new GroupUser
        {
            Id = Guid.NewGuid(),
            UserId = userIdToAdd,
            GroupId = groupId,
            Role = role,
            JoinedAt = DateTime.UtcNow,
            InvitedBy = addedById
        };

        _db.GroupUsers.Add(groupUser);
        await _db.SaveChangesAsync();

        return groupUser;
    }

    public async Task RemoveUserAsync(Guid groupId, Guid userIdToRemove, Guid removedById)
    {
        var isAdmin = await IsGroupAdminAsync(removedById, groupId);
        if (!isAdmin)
            throw new UnauthorizedAccessException("Solo un Admin puede eliminar miembros del grupo.");

        var groupUser = await _db.GroupUsers
            .FirstOrDefaultAsync(gu => gu.GroupId == groupId && gu.UserId == userIdToRemove)
            ?? throw new KeyNotFoundException("El usuario no es miembro del grupo.");

        // Check if the user being removed is the only Admin
        if (groupUser.Role == "Admin")
        {
            var adminCount = await _db.GroupUsers
                .CountAsync(gu => gu.GroupId == groupId && gu.Role == "Admin" && gu.UserId != userIdToRemove);

            if (adminCount == 0)
                throw new InvalidOperationException("No se puede eliminar al último Admin del grupo.");
        }

        _db.GroupUsers.Remove(groupUser);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> IsGroupMemberAsync(Guid userId, Guid groupId)
    {
        return await _db.GroupUsers
            .AnyAsync(gu => gu.UserId == userId && gu.GroupId == groupId);
    }

    public async Task<bool> IsGroupAdminAsync(Guid userId, Guid groupId)
    {
        return await _db.GroupUsers
            .AnyAsync(gu => gu.UserId == userId && gu.GroupId == groupId && gu.Role == "Admin");
    }

    #region Private Methods

    private static GroupResponse MapToGroupResponse(Group group)
    {
        var members = group.GroupUsers
            .OrderBy(gu => gu.JoinedAt)
            .Select(gu => new GroupUserResponse(
                gu.Id,
                gu.UserId,
                gu.User.Name,
                gu.Role,
                gu.JoinedAt))
            .ToList();

        return new GroupResponse(group.Id, group.Name, group.CreatedBy, group.CreatedAt, members);
    }

    private async Task<GroupResponse> MapToGroupResponseAsync(Group group)
    {
        await _db.Entry(group)
            .Collection(g => g.GroupUsers)
            .Query()
            .Include(gu => gu.User)
            .LoadAsync();

        return MapToGroupResponse(group);
    }

    #endregion
}
