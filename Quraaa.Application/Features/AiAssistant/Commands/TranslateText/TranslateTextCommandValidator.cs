using FluentValidation;

namespace Quraaa.Application.Features.AiAssistant.Commands.TranslateText
{
    public sealed class TranslateTextCommandValidator : AbstractValidator<TranslateTextCommand>
    {
        private static readonly HashSet<string> SupportedLanguages =
            new(StringComparer.OrdinalIgnoreCase) { "ar", "en" };

        public TranslateTextCommandValidator()
        {
            RuleFor(x => x.Text)
                .NotEmpty()
                .WithMessage("Text is required.")
                .MaximumLength(5_000)
                .WithMessage("Selected text is too long to translate in one request (max 5,000 characters).");

            RuleFor(x => x.TargetLanguage)
                .Must(lang => SupportedLanguages.Contains(lang))
                .WithMessage("TargetLanguage must be 'ar' or 'en' — the two languages FR-AI-02 scopes this to for MVP.");
        }
    }
}