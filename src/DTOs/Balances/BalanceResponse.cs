namespace back_api_splitwise.src.DTOs.Balances;

public record BalanceResponse(Guid GroupId, List<UserBalanceResponse> Balances, List<DebtResponse> SimplifiedDebts);
