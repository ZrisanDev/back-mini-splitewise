namespace back_api_splitwise.src.DTOs.Expenses;

public record ExpenseSplitRequest(Guid UserId, decimal Amount);
