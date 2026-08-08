using FluentValidation;

namespace Quraaa.Application.Features.AiAssistant.Commands.TranslateText
{
    public sealed class TranslateTextCommandValidator : AbstractValidator<TranslateTextCommand>
    {
        private static readonly HashSet<string> SupportedLanguages =
            new(StringComparer.OrdinalIgnoreCase) { "ar", "en" };

        public TranslateTextCommandValidator()
        {
            RuleFor(x => x.PurchaseId)
                .NotEmpty()
                .WithMessage("PurchaseId is required.");

            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("PageNumber must be a positive, 1-based page number.");

            RuleFor(x => x.TargetLanguage)
                .Must(lang => SupportedLanguages.Contains(lang))
                .WithMessage("TargetLanguage must be 'ar' or 'en' — the two languages FR-AI-02 scopes this to for MVP.");
        }
    }
}