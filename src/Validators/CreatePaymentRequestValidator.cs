using back_api_splitwise.src.DTOs.Payments;
using FluentValidation;

namespace back_api_splitwise.src.Validators;

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.FromUserId)
            .NotEmpty().WithMessage("El ID del usuario que paga es obligatorio.");

        RuleFor(x => x.ToUserId)
            .NotEmpty().WithMessage("El ID del usuario que recibe es obligatorio.")
            .NotEqual(x => x.FromUserId)
            .WithMessage("El usuario que paga y el que recibe no pueden ser el mismo.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0.")
            .PrecisionScale(18, 2, false).WithMessage("El monto no puede tener más de 2 decimales.");

        RuleFor(x => x.Note)
            .MaximumLength(255).WithMessage("La nota no puede superar los 255 caracteres.")
            .When(x => x.Note is not null);
    }
}
