using back_api_splitwise.src.DTOs.Expenses;
using FluentValidation;

namespace back_api_splitwise.src.Validators;

public class UpdateExpenseRequestValidator : AbstractValidator<UpdateExpenseRequest>
{
    public UpdateExpenseRequestValidator()
    {
        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage("La descripción no puede superar los 255 caracteres.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0.")
            .PrecisionScale(18, 2, false).WithMessage("El monto no puede tener más de 2 decimales.")
            .When(x => x.Amount.HasValue);
    }
}
