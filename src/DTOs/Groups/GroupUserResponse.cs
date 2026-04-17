namespace back_api_splitwise.src.DTOs.Groups;

public record GroupUserResponse(Guid Id, Guid UserId, string UserName, string Role, DateTime JoinedAt);
