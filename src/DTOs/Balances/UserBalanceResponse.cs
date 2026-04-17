namespace back_api_splitwise.src.DTOs.Balances;

public record UserBalanceResponse(Guid UserId, string UserName, decimal NetBalance);
