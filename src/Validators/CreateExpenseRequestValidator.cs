using back_api_splitwise.src.DTOs.Expenses;
using FluentValidation;

namespace back_api_splitwise.src.Validators;

public class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(255).WithMessage("La descripción no puede superar los 255 caracteres.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0.")
            .PrecisionScale(18, 2, false).WithMessage("El monto no puede tener más de 2 decimales.");

        RuleFor(x => x.PaidBy)
            .NotEmpty().WithMessage("El ID de quien pagó es obligatorio.");

        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("El ID del grupo es obligatorio.");

        RuleFor(x => x.CreatedBy)
            .NotEmpty().WithMessage("El ID del creador es obligatorio.");

        RuleFor(x => x.SplitType)
            .NotEmpty().WithMessage("El tipo de división es obligatorio.")
            .Must(splitType => splitType is "equal" or "custom")
            .WithMessage("El tipo de división debe ser 'equal' o 'custom'.");

        RuleFor(x => x.Splits)
            .NotEmpty().WithMessage("Los montos individuales son obligatorios cuando el tipo de división es 'custom'.")
            .When(x => x.SplitType == "custom");

        RuleFor(x => x)
            .Custom((request, context) =>
            {
                if (request.SplitType == "custom" && request.Splits is { Count: > 0 })
                {
                    var splitsSum = request.Splits.Sum(s => s.Amount);

                    // Compare with a tolerance for decimal precision
                    if (Math.Abs(splitsSum - request.Amount) > 0.01m)
                    {
                        context.AddFailure(
                            nameof(request.Splits),
                            $"La suma de las divisiones ({splitsSum:F2}) debe ser igual al monto del gasto ({request.Amount:F2}).");
                    }

                    // Check for duplicate user IDs in splits
                    var duplicateUserIds = request.Splits
                        .GroupBy(s => s.UserId)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key);

                    if (duplicateUserIds.Any())
                    {
                        context.AddFailure(
                            nameof(request.Splits),
                            "No se permiten usuarios duplicados en las divisiones.");
                    }

                    // Check for negative or zero split amounts
                    if (request.Splits.Any(s => s.Amount <= 0))
                    {
                        context.AddFailure(
                            nameof(request.Splits),
                            "Todos los montos individuales deben ser mayores a 0.");
                    }
                }
            });
    }
}
