using back_api_splitwise.src.DTOs.Expenses;
using back_api_splitwise.src.DTOs.Pagination;

namespace back_api_splitwise.src.Services.Interfaces;

public interface IExpenseService
{
    Task<ExpenseResponse> CreateAsync(CreateExpenseRequest request, Guid currentUserId);
    Task<PagedResponse<ExpenseResponse>> GetByGroupAsync(Guid groupId, int page, int pageSize, Guid currentUserId);
    Task<ExpenseResponse?> GetByIdAsync(Guid id, Guid currentUserId);
    Task<ExpenseResponse> UpdateAsync(Guid id, UpdateExpenseRequest request, Guid currentUserId);
    Task DeleteAsync(Guid id, Guid currentUserId);
}
