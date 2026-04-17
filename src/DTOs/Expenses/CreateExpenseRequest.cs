namespace back_api_splitwise.src.DTOs.Expenses;

public record CreateExpenseRequest(
    string Description,
    decimal Amount,
    Guid PaidBy,
    Guid GroupId,
    Guid CreatedBy,
    string SplitType,
    List<ExpenseSplitRequest>? Splits);
