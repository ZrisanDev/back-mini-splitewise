namespace back_api_splitwise.src.DTOs.Auth;

public record AuthResponse(Guid UserId, string Email, string Name, string AccessToken, string RefreshToken, int ExpiresIn);
