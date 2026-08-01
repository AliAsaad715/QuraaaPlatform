using FluentValidation;

namespace Quraaa.Application.Features.Authentication.Commands.RefreshToken
{
    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token is required.")
                .MaximumLength(512).WithMessage("Refresh token is invalid.");
        }
    }
}
