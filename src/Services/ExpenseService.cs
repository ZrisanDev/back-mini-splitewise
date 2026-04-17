using Microsoft.EntityFrameworkCore;
using back_api_splitwise.src.Data;
using back_api_splitwise.src.DTOs.Expenses;
using back_api_splitwise.src.DTOs.Pagination;
using back_api_splitwise.src.Entities;
using back_api_splitwise.src.Extensions;
using back_api_splitwise.src.Services.Interfaces;

namespace back_api_splitwise.src.Services;

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _db;

    public ExpenseService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ExpenseResponse> CreateAsync(CreateExpenseRequest request, Guid currentUserId)
    {
        // Validate user is member of the group
        var isMember = await _db.GroupUsers
            .AnyAsync(gu => gu.GroupId == request.GroupId && gu.UserId == request.PaidBy);
        if (!isMember)
            throw new UnauthorizedAccessException("El pagador no es miembro del grupo.");

        // Get all group members for splits
        var groupMembers = await _db.GroupUsers
            .Where(gu => gu.GroupId == request.GroupId)
            .Select(gu => gu.UserId)
            .ToListAsync();

        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var expense = new Expense
            {
                Id = Guid.NewGuid(),
                Description = request.Description,
                Amount = request.Amount,
                PaidBy = request.PaidBy,
                CreatedBy = request.CreatedBy,
                GroupId = request.GroupId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync();

            var splits = GenerateSplits(expense.Id, request.Amount, request.SplitType, request.Splits, groupMembers, request.PaidBy);

            _db.ExpenseSplits.AddRange(splits);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            // Reload with navigation properties for response
            var createdExpense = await _db.Expenses
                .Include(e => e.Splits)
                    .ThenInclude(s => s.User)
                .Include(e => e.PaidByUser)
                .FirstAsync(e => e.Id == expense.Id);

            return MapToExpenseResponse(createdExpense);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PagedResponse<ExpenseResponse>> GetByGroupAsync(Guid groupId, int page, int pageSize, Guid currentUserId)
    {
        var isMember = await _db.GroupUsers
            .AnyAsync(gu => gu.GroupId == groupId && gu.UserId == currentUserId);
        if (!isMember)
            throw new UnauthorizedAccessException("No sos miembro de este grupo.");

        var query = _db.Expenses
            .Where(e => e.GroupId == groupId)
            .Include(e => e.Splits)
                .ThenInclude(s => s.User)
            .Include(e => e.PaidByUser);

        var totalCount = await query.CountAsync();
        var expenses = await query
            .OrderByDescending(e => e.CreatedAt)
            .Paginate(page, pageSize)
            .ToListAsync();

        var responses = expenses.Select(MapToExpenseResponse).ToList();
        return responses.ToPagedResponse(page, pageSize, totalCount);
    }

    public async Task<ExpenseResponse?> GetByIdAsync(Guid id, Guid currentUserId)
    {
        var expense = await _db.Expenses
            .Include(e => e.Splits)
                .ThenInclude(s => s.User)
            .Include(e => e.PaidByUser)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (expense is null)
            return null;

        var isMember = await _db.GroupUsers
            .AnyAsync(gu => gu.GroupId == expense.GroupId && gu.UserId == currentUserId);
        if (!isMember)
            throw new UnauthorizedAccessException("No sos miembro del grupo de este gasto.");

        return MapToExpenseResponse(expense);
    }

    public async Task<ExpenseResponse> UpdateAsync(Guid id, UpdateExpenseRequest request, Guid currentUserId)
    {
        var expense = await _db.Expenses
            .Include(e => e.Splits)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException("Gasto no encontrado.");

        if (expense.CreatedBy != currentUserId)
            throw new UnauthorizedAccessException("Solo el creador puede editar el gasto.");

        // Update fields if provided
        if (request.Description is not null)
            expense.Description = request.Description;

        expense.UpdatedAt = DateTime.UtcNow;

        // If amount changed, recalculate splits
        if (request.Amount.HasValue && request.Amount != expense.Amount)
        {
            var groupMembers = await _db.GroupUsers
                .Where(gu => gu.GroupId == expense.GroupId)
                .Select(gu => gu.UserId)
                .ToListAsync();

            // Remove old splits
            _db.ExpenseSplits.RemoveRange(expense.Splits);
            expense.Amount = request.Amount.Value;

            var newSplits = GenerateSplits(
                expense.Id,
                expense.Amount,
                "equal", // Update recalculates as equal split
                null,
                groupMembers,
                expense.PaidBy);

            _db.ExpenseSplits.AddRange(newSplits);
        }

        await _db.SaveChangesAsync();

        // Reload for response
        var updatedExpense = await _db.Expenses
            .Include(e => e.Splits)
                .ThenInclude(s => s.User)
            .Include(e => e.PaidByUser)
            .FirstAsync(e => e.Id == expense.Id);

        return MapToExpenseResponse(updatedExpense);
    }

    public async Task DeleteAsync(Guid id, Guid currentUserId)
    {
        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException("Gasto no encontrado.");

        if (expense.CreatedBy != currentUserId)
            throw new UnauthorizedAccessException("Solo el creador puede eliminar el gasto.");

        expense.IsDeleted = true;
        expense.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    #region Private Methods

    private static List<ExpenseSplit> GenerateSplits(
        Guid expenseId,
        decimal amount,
        string splitType,
        List<ExpenseSplitRequest>? customSplits,
        List<Guid> groupMembers,
        Guid paidBy)
    {
        var splits = new List<ExpenseSplit>();

        if (splitType.Equals("custom", StringComparison.OrdinalIgnoreCase) && customSplits is { Count: > 0 })
        {
            foreach (var split in customSplits)
            {
                splits.Add(new ExpenseSplit
                {
                    Id = Guid.NewGuid(),
                    ExpenseId = expenseId,
                    UserId = split.UserId,
                    Amount = split.Amount
                });
            }
        }
        else
        {
            // Equal split: divide evenly among all members, remainder goes to payer
            var share = Math.Floor(amount * 100 / groupMembers.Count) / 100;
            var distributed = share * groupMembers.Count;
            var remainder = amount - distributed;

            foreach (var memberId in groupMembers)
            {
                var splitAmount = memberId == paidBy ? share + remainder : share;

                splits.Add(new ExpenseSplit
                {
                    Id = Guid.NewGuid(),
                    ExpenseId = expenseId,
                    UserId = memberId,
                    Amount = splitAmount
                });
            }
        }

        return splits;
    }

    private static ExpenseResponse MapToExpenseResponse(Expense expense)
    {
        var splits = expense.Splits
            .Select(s => new ExpenseSplitResponse(
                s.Id,
                s.UserId,
                s.User.Name,
                s.Amount,
                s.IsSettled,
                s.SettledAt))
            .ToList();

        return new ExpenseResponse(
            expense.Id,
            expense.Description,
            expense.Amount,
            expense.PaidBy,
            expense.PaidByUser.Name,
            expense.GroupId,
            expense.CreatedAt,
            splits);
    }

    #endregion
}
