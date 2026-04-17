namespace back_api_splitwise.src.DTOs.Expenses;

public record ExpenseSplitResponse(Guid Id, Guid UserId, string UserName, decimal Amount, bool IsSettled, DateTime? SettledAt);
