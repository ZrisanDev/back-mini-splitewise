namespace back_api_splitwise.src.DTOs.Payments;

public record PaymentResponse(
    Guid Id,
    Guid FromUserId,
    string FromUserName,
    Guid ToUserId,
    string ToUserName,
    decimal Amount,
    string? Note,
    DateTime CreatedAt);
