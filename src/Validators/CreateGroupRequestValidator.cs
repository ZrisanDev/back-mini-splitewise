using back_api_splitwise.src.DTOs.Groups;
using FluentValidation;

namespace back_api_splitwise.src.Validators;

public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del grupo es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre del grupo no puede superar los 100 caracteres.");
    }
}
