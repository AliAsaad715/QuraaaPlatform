using FluentValidation;

namespace Quraaa.Application.Features.AiAssistant.Commands.SummarizeText
{
    public sealed class SummarizeTextCommandValidator : AbstractValidator<SummarizeTextCommand>
    {
        public SummarizeTextCommandValidator()
        {
            RuleFor(x => x.Text)
                .NotEmpty()
                .WithMessage("Text is required.")
                .MaximumLength(20_000)
                .WithMessage("Selected text is too long to summarize in one request (max 20,000 characters — a chapter or so). Assumed cap, not specified in the requirement; tune freely.");
        }
    }
}