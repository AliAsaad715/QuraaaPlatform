using FluentValidation;

namespace Quraaa.Application.Features.Authors.Commands.UpdateAuthor
{
    public class UpdateAuthorCommandValidator : AbstractValidator<UpdateAuthorCommand>
    {
        public UpdateAuthorCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Author id is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Author name is required.")
                .MaximumLength(150).WithMessage("Author name must not exceed 150 characters.");

            RuleFor(x => x.Bio)
                .MaximumLength(2000).WithMessage("Author bio must not exceed 2000 characters.");

            RuleFor(x => x.PhotoUrl)
                .MaximumLength(500).WithMessage("Photo URL must not exceed 500 characters.")
                .Must(BeAValidAbsoluteUrl).WithMessage("Photo URL must be a valid absolute HTTP/HTTPS URL.")
                .When(x => !string.IsNullOrWhiteSpace(x.PhotoUrl));

            RuleFor(x => x.BirthDate)
                .LessThan(_ => DateTime.UtcNow).WithMessage("Birth date must be in the past.")
                .When(x => x.BirthDate.HasValue);
        }

        private static bool BeAValidAbsoluteUrl(string? url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
