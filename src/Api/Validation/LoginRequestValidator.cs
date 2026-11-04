using DotnetSecureApi.Api.Controllers;
using FluentValidation;

namespace DotnetSecureApi.Api.Validation;

/// <summary>
/// Validator for the <see cref="LoginRequest"/> model using FluentValidation.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required.")
            .MaximumLength(50)
            .WithMessage("Username cannot be longer than 50 characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.");
    }
}
