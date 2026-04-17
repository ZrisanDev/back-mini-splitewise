using System.Text.Json;
using System.Text.Json.Serialization;

namespace back_api_splitwise.src.Extensions;

/// <summary>
/// Global exception handler that maps service-layer exceptions to proper HTTP responses.
/// Must be registered AFTER UseAuthentication/UseAuthorization so auth failures are
/// handled by the framework (not intercepted here).
/// </summary>
public static class ExceptionHandlingExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static WebApplication UseGlobalExceptionHandler(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (UnauthorizedAccessException ex)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(SerializeProblem(
                    "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                    "Forbidden",
                    ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(SerializeProblem(
                    "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                    "Not Found",
                    ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(SerializeProblem(
                    "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    "Bad Request",
                    ex.Message));
            }
        });

        return app;
    }

    private static string SerializeProblem(string type, string title, string detail)
    {
        return JsonSerializer.Serialize(new { type, title, detail }, _jsonOptions);
    }
}
