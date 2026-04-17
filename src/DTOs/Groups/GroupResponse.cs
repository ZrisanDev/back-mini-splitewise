namespace back_api_splitwise.src.DTOs.Groups;

public record GroupResponse(Guid Id, string Name, Guid CreatedBy, DateTime CreatedAt, List<GroupUserResponse> Members);
