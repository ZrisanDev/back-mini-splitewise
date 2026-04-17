namespace back_api_splitwise.src.DTOs.Expenses;

public record UpdateExpenseRequest(string? Description, decimal? Amount);
