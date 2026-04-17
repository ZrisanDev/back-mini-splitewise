namespace back_api_splitwise.src.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public Guid GroupId { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User FromUser { get; set; } = null!;
    public User ToUser { get; set; } = null!;
    public Group Group { get; set; } = null!;
}
