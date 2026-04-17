namespace back_api_splitwise.src.DTOs.Expenses;

public record ExpenseResponse(
    Guid Id,
    string Description,
    decimal Amount,
    Guid PaidBy,
    string PaidByName,
    Guid GroupId,
    DateTime CreatedAt,
    List<ExpenseSplitResponse> Splits);
