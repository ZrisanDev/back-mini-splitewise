using System.Security.Claims;
using back_api_splitwise.src.Data;
using back_api_splitwise.src.DTOs.Auth;
using back_api_splitwise.src.DTOs.Balances;
using back_api_splitwise.src.DTOs.Expenses;
using back_api_splitwise.src.DTOs.Groups;
using back_api_splitwise.src.DTOs.Pagination;
using back_api_splitwise.src.DTOs.Payments;
using back_api_splitwise.src.DTOs.Users;
using back_api_splitwise.src.Extensions;
using back_api_splitwise.src.Helpers;
using back_api_splitwise.src.Services.Interfaces;
using FluentValidation;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddApplicationServices(builder.Configuration);

// ── Swagger / OpenAPI ────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Splitwise API",
        Version = "v1",
        Description = "API para gestión de gastos compartidos entre grupos de personas. " +
                      "Soporta autenticación JWT, gestión de grupos, gastos con splits " +
                      "(iguales o personalizados), pagos y cálculo automático de balances/deudas simplificadas."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT obtenido del endpoint /api/auth/login. " +
                      "Formato: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Middleware Pipeline ──────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseGlobalExceptionHandler();

// ══════════════════════════════════════════════════════════════════════════════
// Auth Endpoints (AllowAnonymous)
// ══════════════════════════════════════════════════════════════════════════════

var authGroup = app.MapGroup("/api/auth").AllowAnonymous();

authGroup.MapPost("/register", async (
    RegisterRequest request,
    [FromService] IValidator<RegisterRequest> validator,
    IAuthService authService) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var user = await authService.RegisterAsync(request.Name, request.Email, request.Password);

    return Results.Created($"/api/users/{user.Id}", new UserResponse(
        user.Id, user.Name, user.Email, user.CreatedAt));
})
.WithName("Register");

authGroup.MapPost("/login", async (
    LoginRequest request,
    [FromService] IValidator<LoginRequest> validator,
    IAuthService authService) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var (accessToken, refreshToken) = await authService.LoginAsync(request.Email, request.Password);

    return Results.Ok(new LoginResponse(accessToken, refreshToken, ExpiresIn: 900));
})
.WithName("Login");

authGroup.MapPost("/refresh", async (
    RefreshRequest request,
    [FromService] IValidator<RefreshRequest> validator,
    IAuthService authService) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var (accessToken, refreshToken) = await authService.RefreshTokenAsync(request.RefreshToken);

    return Results.Ok(new LoginResponse(accessToken, refreshToken, ExpiresIn: 900));
})
.WithName("RefreshToken");

authGroup.MapPost("/logout", async (
    RefreshRequest request,
    IAuthService authService) =>
{
    await authService.LogoutAsync(request.RefreshToken);

    return Results.NoContent();
})
.WithName("Logout");

// ══════════════════════════════════════════════════════════════════════════════
// User Endpoints (Protected)
// ══════════════════════════════════════════════════════════════════════════════

var usersGroup = app.MapGroup("/api/users").RequireAuthorization();

usersGroup.MapGet("/me", async (
    ClaimsPrincipal user,
    IAuthService authService) =>
{
    var userId = user.GetUserId();
    var foundUser = await authService.GetUserByIdAsync(userId);

    return foundUser is null
        ? Results.NotFound(new { error = "Usuario no encontrado." })
        : Results.Ok(new UserResponse(foundUser.Id, foundUser.Name, foundUser.Email, foundUser.CreatedAt));
})
.WithName("GetCurrentUser");

usersGroup.MapPut("/me", async (
    ClaimsPrincipal user,
    UpdateUserRequest request,
    [FromService] IValidator<UpdateUserRequest> validator,
    AppDbContext db) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var userId = user.GetUserId();
    var foundUser = await db.Users.FindAsync(userId);

    if (foundUser is null)
        return Results.NotFound(new { error = "Usuario no encontrado." });

    foundUser.Name = request.Name;
    foundUser.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("UpdateCurrentUser");

usersGroup.MapPut("/me/password", async (
    ClaimsPrincipal user,
    ChangePasswordRequest request,
    [FromService] IValidator<ChangePasswordRequest> validator,
    AppDbContext db) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var userId = user.GetUserId();
    var foundUser = await db.Users.FindAsync(userId);

    if (foundUser is null)
        return Results.NotFound(new { error = "Usuario no encontrado." });

    if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, foundUser.PasswordHash))
        return Results.Problem("La contraseña actual es incorrecta.", statusCode: 400);

    foundUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 10);
    foundUser.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("ChangePassword");

// ══════════════════════════════════════════════════════════════════════════════
// Group Endpoints (Protected)
// ══════════════════════════════════════════════════════════════════════════════

var groupsGroup = app.MapGroup("/api/groups").RequireAuthorization();

groupsGroup.MapGet("/", async (
    ClaimsPrincipal user,
    IGroupService groupService,
    [AsParameters] PaginationParams pagination) =>
{
    var userId = user.GetUserId();
    var result = await groupService.GetByUserAsync(userId, pagination.Page, pagination.PageSize);
    return Results.Ok(result);
})
.WithName("GetGroups");

groupsGroup.MapPost("/", async (
    ClaimsPrincipal user,
    CreateGroupRequest request,
    [FromService] IValidator<CreateGroupRequest> validator,
    IGroupService groupService) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var userId = user.GetUserId();
    var group = await groupService.CreateAsync(request.Name, userId);

    return Results.Created($"/api/groups/{group.Id}", group);
})
.WithName("CreateGroup");

groupsGroup.MapGet("/{id}", async (
    Guid id,
    ClaimsPrincipal user,
    IGroupService groupService) =>
{
    var userId = user.GetUserId();
    var group = await groupService.GetByIdAsync(id, userId);

    return group is null
        ? Results.NotFound(new { error = "Grupo no encontrado." })
        : Results.Ok(group);
})
.WithName("GetGroupById");

groupsGroup.MapDelete("/{id}", async (
    Guid id,
    ClaimsPrincipal user,
    IGroupService groupService) =>
{
    var userId = user.GetUserId();
    await groupService.DeleteAsync(id, userId);

    return Results.NoContent();
})
.WithName("DeleteGroup");

groupsGroup.MapPost("/{id}/users", async (
    Guid id,
    ClaimsPrincipal user,
    AddGroupUserRequest request,
    [FromService] IValidator<AddGroupUserRequest> validator,
    IGroupService groupService) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var userId = user.GetUserId();
    await groupService.AddUserAsync(id, request.UserId, request.Role, userId);

    return Results.NoContent();
})
.WithName("AddGroupUser");

groupsGroup.MapDelete("/{id}/users/{userId}", async (
    Guid id,
    Guid userId,
    ClaimsPrincipal user,
    IGroupService groupService) =>
{
    var currentUserId = user.GetUserId();
    await groupService.RemoveUserAsync(id, userId, currentUserId);

    return Results.NoContent();
})
.WithName("RemoveGroupUser");

// ══════════════════════════════════════════════════════════════════════════════
// Group Sub-resource Endpoints (Protected)
// ══════════════════════════════════════════════════════════════════════════════

// GET /api/groups/{groupId}/expenses — list expenses by group (paginated)
var groupExpensesGroup = app.MapGroup("/api/groups/{groupId}/expenses").RequireAuthorization();

groupExpensesGroup.MapGet("/", async (
    Guid groupId,
    ClaimsPrincipal user,
    IExpenseService expenseService,
    [AsParameters] PaginationParams pagination) =>
{
    var userId = user.GetUserId();
    var result = await expenseService.GetByGroupAsync(groupId, pagination.Page, pagination.PageSize, userId);
    return Results.Ok(result);
})
.WithName("GetGroupExpenses");

// POST /api/groups/{groupId}/payments — create payment
var groupPaymentsGroup = app.MapGroup("/api/groups/{groupId}/payments").RequireAuthorization();

groupPaymentsGroup.MapPost("/", async (
    Guid groupId,
    ClaimsPrincipal user,
    CreatePaymentRequest request,
    [FromService] IValidator<CreatePaymentRequest> validator,
    IPaymentService paymentService) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var userId = user.GetUserId();
    var payment = await paymentService.CreateAsync(groupId, request, userId);

    return Results.Created($"/api/payments/{payment.Id}", payment);
})
.WithName("CreatePayment");

// GET /api/groups/{groupId}/payments — list payments by group (paginated)
groupPaymentsGroup.MapGet("/", async (
    Guid groupId,
    ClaimsPrincipal user,
    IPaymentService paymentService,
    [AsParameters] PaginationParams pagination) =>
{
    var userId = user.GetUserId();
    var result = await paymentService.GetByGroupAsync(groupId, pagination.Page, pagination.PageSize, userId);
    return Results.Ok(result);
})
.WithName("GetGroupPayments");

// GET /api/groups/{groupId}/balances — get balances and simplified debts
var groupBalancesGroup = app.MapGroup("/api/groups/{groupId}/balances").RequireAuthorization();

groupBalancesGroup.MapGet("/", async (
    Guid groupId,
    ClaimsPrincipal user,
    IBalanceService balanceService) =>
{
    var userId = user.GetUserId();
    var result = await balanceService.GetBalancesAsync(groupId, userId);
    return Results.Ok(result);
})
.WithName("GetGroupBalances");

// ══════════════════════════════════════════════════════════════════════════════
// Expense Endpoints (Protected)
// ══════════════════════════════════════════════════════════════════════════════

var expensesGroup = app.MapGroup("/api/expenses").RequireAuthorization();

expensesGroup.MapPost("/", async (
    ClaimsPrincipal user,
    CreateExpenseRequest request,
    [FromService] IValidator<CreateExpenseRequest> validator,
    IExpenseService expenseService) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var userId = user.GetUserId();
    var expense = await expenseService.CreateAsync(request, userId);

    return Results.Created($"/api/expenses/{expense.Id}", expense);
})
.WithName("CreateExpense");

expensesGroup.MapGet("/{id}", async (
    Guid id,
    ClaimsPrincipal user,
    IExpenseService expenseService) =>
{
    var userId = user.GetUserId();
    var expense = await expenseService.GetByIdAsync(id, userId);

    return expense is null
        ? Results.NotFound(new { error = "Gasto no encontrado." })
        : Results.Ok(expense);
})
.WithName("GetExpenseById");

expensesGroup.MapPut("/{id}", async (
    Guid id,
    ClaimsPrincipal user,
    UpdateExpenseRequest request,
    [FromService] IValidator<UpdateExpenseRequest> validator,
    IExpenseService expenseService) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    var userId = user.GetUserId();
    var expense = await expenseService.UpdateAsync(id, request, userId);

    return Results.Ok(expense);
})
.WithName("UpdateExpense");

expensesGroup.MapDelete("/{id}", async (
    Guid id,
    ClaimsPrincipal user,
    IExpenseService expenseService) =>
{
    var userId = user.GetUserId();
    await expenseService.DeleteAsync(id, userId);

    return Results.NoContent();
})
.WithName("DeleteExpense");

app.Run();
