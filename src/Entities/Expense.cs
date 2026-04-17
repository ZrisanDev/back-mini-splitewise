namespace back_api_splitwise.src.Entities;

public class Expense
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid PaidBy { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid GroupId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<ExpenseSplit> Splits { get; set; } = new List<ExpenseSplit>();
    public Group Group { get; set; } = null!;
    public User PaidByUser { get; set; } = null!;
}
