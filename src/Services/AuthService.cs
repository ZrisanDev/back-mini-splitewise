using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using back_api_splitwise.src.Data;
using back_api_splitwise.src.Entities;
using back_api_splitwise.src.Services.Interfaces;

namespace back_api_splitwise.src.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<User> RegisterAsync(string name, string email, string password)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existing is not null)
            throw new InvalidOperationException("El email ya está registrado.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 10),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return user;
    }

    public async Task<(string AccessToken, string RefreshToken)> LoginAsync(string email, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales inválidas.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("El usuario está inactivo.");

        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        await SaveRefreshTokenAsync(user.Id, refreshToken);

        return (accessToken, refreshToken);
    }

    public async Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt =>
                !rt.IsRevoked &&
                rt.ExpiresAt > DateTime.UtcNow);

        if (storedToken is null || !BCrypt.Net.BCrypt.Verify(refreshToken, storedToken.Token))
            throw new UnauthorizedAccessException("Refresh token inválido o expirado.");

        if (!storedToken.User.IsActive)
            throw new UnauthorizedAccessException("El usuario está inactivo.");

        // Revoke the old token
        storedToken.IsRevoked = true;
        await _db.SaveChangesAsync();

        var newAccessToken = GenerateAccessToken(storedToken.User);
        var newRefreshToken = GenerateRefreshToken();

        await SaveRefreshTokenAsync(storedToken.UserId, newRefreshToken);

        return (newAccessToken, newRefreshToken);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var storedTokens = await _db.RefreshTokens
            .Where(rt => !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in storedTokens)
        {
            if (BCrypt.Net.BCrypt.Verify(refreshToken, token.Token))
            {
                token.IsRevoked = true;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
    }

    #region Private Methods

    private string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(randomBytes);
    }

    private async Task SaveRefreshTokenAsync(Guid userId, string token)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = BCrypt.Net.BCrypt.HashPassword(token, 10),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();
    }

    #endregion
}
