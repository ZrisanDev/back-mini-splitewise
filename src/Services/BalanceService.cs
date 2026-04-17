using Microsoft.EntityFrameworkCore;
using back_api_splitwise.src.Data;
using back_api_splitwise.src.DTOs.Balances;
using back_api_splitwise.src.Helpers;
using back_api_splitwise.src.Services.Interfaces;

namespace back_api_splitwise.src.Services;

public class BalanceService : IBalanceService
{
    private readonly AppDbContext _db;

    public BalanceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<BalanceResponse> GetBalancesAsync(Guid groupId, Guid currentUserId)
    {
        // Verify user is member of the group
        var isMember = await _db.GroupUsers
            .AnyAsync(gu => gu.GroupId == groupId && gu.UserId == currentUserId);
        if (!isMember)
            throw new UnauthorizedAccessException("No sos miembro de este grupo.");

        // Get all group members
        var members = await _db.GroupUsers
            .Include(gu => gu.User)
            .Where(gu => gu.GroupId == groupId)
            .ToListAsync();

        var balances = new List<UserBalanceResponse>();

        foreach (var member in members)
        {
            var netBalance = await CalculateNetBalanceAsync(member.UserId, groupId);
            balances.Add(new UserBalanceResponse(
                member.UserId,
                member.User.Name,
                netBalance));
        }

        var simplifiedDebts = DebtSimplifier.Simplify(balances);

        return new BalanceResponse(groupId, balances, simplifiedDebts);
    }

    #region Private Methods

    /// <summary>
    /// Calculates the net balance for a user in a group.
    /// Formula: balance = SUM(expenses paid) - SUM(expense splits owed) + SUM(payments received) - SUM(payments sent)
    /// Positive = user is owed money. Negative = user owes money.
    /// </summary>
    private async Task<decimal> CalculateNetBalanceAsync(Guid userId, Guid groupId)
    {
        // Sum of expenses this user paid in the group
        var totalPaid = await _db.Expenses
            .Where(e => e.GroupId == groupId && e.PaidBy == userId)
            .SumAsync(e => e.Amount);

        // Sum of expense splits this user owes in the group
        var totalOwed = await _db.ExpenseSplits
            .Include(s => s.Expense)
            .Where(s => s.Expense.GroupId == groupId && s.UserId == userId)
            .SumAsync(s => s.Amount);

        // Sum of payments this user received in the group
        var totalReceived = await _db.Payments
            .Where(p => p.GroupId == groupId && p.ToUserId == userId)
            .SumAsync(p => p.Amount);

        // Sum of payments this user sent in the group
        var totalSent = await _db.Payments
            .Where(p => p.GroupId == groupId && p.FromUserId == userId)
            .SumAsync(p => p.Amount);

        return totalPaid - totalOwed + totalReceived - totalSent;
    }

    #endregion
}
