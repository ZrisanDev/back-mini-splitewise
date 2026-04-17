using back_api_splitwise.src.DTOs.Balances;

namespace back_api_splitwise.src.Services.Interfaces;

public interface IBalanceService
{
    Task<BalanceResponse> GetBalancesAsync(Guid groupId, Guid currentUserId);
}
