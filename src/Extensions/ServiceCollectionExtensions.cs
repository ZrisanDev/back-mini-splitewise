using System.Text;
using FluentValidation;
using back_api_splitwise.src.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using back_api_splitwise.src.Data;
using back_api_splitwise.src.Services;
using back_api_splitwise.src.Services.Interfaces;

namespace back_api_splitwise.src.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = config.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
            }

            // SQL Server
            if (connectionString.Contains("Server=") || connectionString.Contains("Data Source="))
            {
                options.UseSqlServer(connectionString);
            }
            // PostgreSQL
            else if (connectionString.StartsWith("Host="))
            {
                options.UseNpgsql(connectionString);
            }
            // SQLite (default)
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        // FluentValidation — registers all validators from the assembly
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        // Authentication — JWT Bearer
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(config["Jwt:SecretKey"]!))
                };
            });

        // Authorization
        services.AddAuthorization();

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IBalanceService, BalanceService>();

        return services;
    }
}
