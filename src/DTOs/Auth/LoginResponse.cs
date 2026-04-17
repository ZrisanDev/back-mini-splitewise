namespace back_api_splitwise.src.DTOs.Auth;

public record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn);
