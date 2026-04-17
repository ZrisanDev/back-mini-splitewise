namespace back_api_splitwise.src.DTOs.Groups;

public record AddGroupUserRequest(Guid UserId, string Role);
