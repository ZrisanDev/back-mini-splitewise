namespace back_api_splitwise.src.DTOs.Payments;

public record CreatePaymentRequest(Guid FromUserId, Guid ToUserId, decimal Amount, string? Note);
