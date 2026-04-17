using back_api_splitwise.src.DTOs.Balances;

namespace back_api_splitwise.src.Helpers;

/// <summary>
/// Simplifies debts using a greedy algorithm.
/// Separates creditors (positive balance) and debtors (negative balance),
/// then matches them from largest to smallest to minimize the number of transactions.
/// Time complexity: O(n log n) due to sorting.
/// </summary>
public static class DebtSimplifier
{
    public static List<DebtResponse> Simplify(List<UserBalanceResponse> balances)
    {
        var creditors = balances
            .Where(b => b.NetBalance > 0)
            .OrderByDescending(b => b.NetBalance)
            .ToList();

        var debtors = balances
            .Where(b => b.NetBalance < 0)
            .OrderBy(b => b.NetBalance)
            .ToList();

        var simplified = new List<DebtResponse>();
        var i = 0;
        var j = 0;

        while (i < creditors.Count && j < debtors.Count)
        {
            var creditor = creditors[i];
            var debtor = debtors[j];

            var amount = Math.Min(Math.Abs(creditor.NetBalance), Math.Abs(debtor.NetBalance));

            if (amount > 0.01m)
            {
                simplified.Add(new DebtResponse(
                    debtor.UserId,
                    debtor.UserName,
                    creditor.UserId,
                    creditor.UserName,
                    Math.Round(amount, 2)));
            }

            creditor = creditor with { NetBalance = creditor.NetBalance - amount };
            debtor = debtor with { NetBalance = debtor.NetBalance + amount };

            // Since UserBalanceResponse is a record, reassign to update the sorted lists
            creditors[i] = creditor;
            debtors[j] = debtor;

            if (Math.Abs(creditor.NetBalance) < 0.01m) i++;
            if (Math.Abs(debtor.NetBalance) < 0.01m) j++;
        }

        return simplified;
    }
}
