using back_api_splitwise.src.Entities;

namespace back_api_splitwise.src.Services.Interfaces;

public interface IAuthService
{
    Task<User> RegisterAsync(string name, string email, string password);
    Task<(string AccessToken, string RefreshToken)> LoginAsync(string email, string password);
    Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task<User?> GetUserByIdAsync(Guid userId);
}
