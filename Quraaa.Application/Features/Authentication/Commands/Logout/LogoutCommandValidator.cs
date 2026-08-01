using FluentValidation;

namespace Quraaa.Application.Features.Authentication.Commands.Logout
{
    public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
    {
        public LogoutCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token is required.")
                .MaximumLength(512).WithMessage("Refresh token is invalid.");

            RuleFor(x => x.AccessTokenId)
                .NotEmpty().WithMessage("Access token id is required.")
                .MaximumLength(128).WithMessage("Access token id is invalid.")
                .When(x => x.AccessTokenExpiresAt.HasValue);

            RuleFor(x => x.AccessTokenExpiresAt)
                .NotNull().WithMessage("Access token expiration is required.")
                .When(x => !string.IsNullOrWhiteSpace(x.AccessTokenId));
        }
    }
}
