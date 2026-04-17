namespace back_api_splitwise.src.DTOs.Users;

public record UserResponse(Guid Id, string Name, string Email, DateTime CreatedAt);
