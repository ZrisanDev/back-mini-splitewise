namespace back_api_splitwise.src.DTOs.Balances;

public record DebtResponse(Guid FromUserId, string FromUserName, Guid ToUserId, string ToUserName, decimal Amount);
