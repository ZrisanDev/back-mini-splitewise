using Microsoft.EntityFrameworkCore;
using back_api_splitwise.src.Data;
using back_api_splitwise.src.DTOs.Payments;
using back_api_splitwise.src.DTOs.Pagination;
using back_api_splitwise.src.Entities;
using back_api_splitwise.src.Extensions;
using back_api_splitwise.src.Services.Interfaces;

namespace back_api_splitwise.src.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;

    public PaymentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PaymentResponse> CreateAsync(Guid groupId, CreatePaymentRequest request, Guid currentUserId)
    {
        // Validate both users are members of the group
        var fromMember = await _db.GroupUsers
            .AnyAsync(gu => gu.GroupId == groupId && gu.UserId == request.FromUserId);
        if (!fromMember)
            throw new UnauthorizedAccessException("El emisor del pago no es miembro del grupo.");

        var toMember = await _db.GroupUsers
            .AnyAsync(gu => gu.GroupId == groupId && gu.UserId == request.ToUserId);
        if (!toMember)
            throw new UnauthorizedAccessException("El receptor del pago no es miembro del grupo.");

        // Validate the caller is the one making the payment
        if (request.FromUserId != currentUserId)
            throw new UnauthorizedAccessException("Solo podés registrar pagos que vos enviás.");

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            FromUserId = request.FromUserId,
            ToUserId = request.ToUserId,
            GroupId = groupId,
            Amount = request.Amount,
            Note = request.Note,
            CreatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        // Reload with navigation properties
        var createdPayment = await _db.Payments
            .Include(p => p.FromUser)
            .Include(p => p.ToUser)
            .FirstAsync(p => p.Id == payment.Id);

        return MapToPaymentResponse(createdPayment);
    }

    public async Task<PagedResponse<PaymentResponse>> GetByGroupAsync(Guid groupId, int page, int pageSize, Guid currentUserId)
    {
        var isMember = await _db.GroupUsers
            .AnyAsync(gu => gu.GroupId == groupId && gu.UserId == currentUserId);
        if (!isMember)
            throw new UnauthorizedAccessException("No sos miembro de este grupo.");

        var query = _db.Payments
            .Where(p => p.GroupId == groupId)
            .Include(p => p.FromUser)
            .Include(p => p.ToUser);

        var totalCount = await query.CountAsync();
        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .Paginate(page, pageSize)
            .ToListAsync();

        var responses = payments.Select(MapToPaymentResponse).ToList();
        return responses.ToPagedResponse(page, pageSize, totalCount);
    }

    #region Private Methods

    private static PaymentResponse MapToPaymentResponse(Payment payment)
    {
        return new PaymentResponse(
            payment.Id,
            payment.FromUserId,
            payment.FromUser.Name,
            payment.ToUserId,
            payment.ToUser.Name,
            payment.Amount,
            payment.Note,
            payment.CreatedAt);
    }

    #endregion
}
