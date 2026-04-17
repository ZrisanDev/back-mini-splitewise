using back_api_splitwise.src.DTOs.Groups;
using FluentValidation;

namespace back_api_splitwise.src.Validators;

public class AddGroupUserRequestValidator : AbstractValidator<AddGroupUserRequest>
{
    public AddGroupUserRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El ID del usuario es obligatorio.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("El rol es obligatorio.")
            .Must(role => role is "Admin" or "Member")
            .WithMessage("El rol debe ser 'Admin' o 'Member'.");
    }
}
