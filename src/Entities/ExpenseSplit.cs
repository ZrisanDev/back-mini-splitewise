namespace back_api_splitwise.src.Entities;

public class ExpenseSplit
{
    public Guid Id { get; set; }
    public Guid ExpenseId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public bool IsSettled { get; set; } = false;
    public DateTime? SettledAt { get; set; }

    // Navigation properties
    public Expense Expense { get; set; } = null!;
    public User User { get; set; } = null!;
}
