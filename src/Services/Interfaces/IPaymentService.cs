using back_api_splitwise.src.DTOs.Payments;
using back_api_splitwise.src.DTOs.Pagination;

namespace back_api_splitwise.src.Services.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(Guid groupId, CreatePaymentRequest request, Guid currentUserId);
    Task<PagedResponse<PaymentResponse>> GetByGroupAsync(Guid groupId, int page, int pageSize, Guid currentUserId);
}
