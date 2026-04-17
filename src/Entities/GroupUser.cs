namespace back_api_splitwise.src.Entities;

public class GroupUser
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }
    public string Role { get; set; } = "Member"; // "Admin" | "Member"
    public DateTime JoinedAt { get; set; }
    public Guid? InvitedBy { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Group Group { get; set; } = null!;
}
