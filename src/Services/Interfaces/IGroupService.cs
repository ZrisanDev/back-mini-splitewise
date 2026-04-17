using back_api_splitwise.src.DTOs.Groups;
using back_api_splitwise.src.DTOs.Pagination;
using back_api_splitwise.src.Entities;

namespace back_api_splitwise.src.Services.Interfaces;

public interface IGroupService
{
    Task<GroupResponse> CreateAsync(string name, Guid createdById);
    Task<PagedResponse<GroupResponse>> GetByUserAsync(Guid userId, int page, int pageSize);
    Task<GroupResponse?> GetByIdAsync(Guid id, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
    Task<GroupUser> AddUserAsync(Guid groupId, Guid userIdToAdd, string role, Guid addedById);
    Task RemoveUserAsync(Guid groupId, Guid userIdToRemove, Guid removedById);
    Task<bool> IsGroupMemberAsync(Guid userId, Guid groupId);
    Task<bool> IsGroupAdminAsync(Guid userId, Guid groupId);
}
