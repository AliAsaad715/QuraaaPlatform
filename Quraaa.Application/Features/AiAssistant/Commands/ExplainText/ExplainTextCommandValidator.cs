using FluentValidation;

namespace Quraaa.Application.Features.AiAssistant.Commands.ExplainText
{
    public sealed class ExplainTextCommandValidator : AbstractValidator<ExplainTextCommand>
    {
        public ExplainTextCommandValidator()
        {
            RuleFor(x => x.PurchaseId)
                .NotEmpty()
                .WithMessage("PurchaseId is required.");

            RuleFor(x => x.SelectedText)
                .NotEmpty()
                .WithMessage("SelectedText is required.")
                .MaximumLength(500)
                .WithMessage("Explain is meant for a single word or sentence (max 500 characters) — use summarize for longer passages.");
        }
    }
}