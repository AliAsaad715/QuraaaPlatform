using FluentValidation;

namespace Quraaa.Application.Features.AiAssistant.Commands.SummarizeText
{
    public sealed class SummarizeTextCommandValidator : AbstractValidator<SummarizeTextCommand>
    {
        public SummarizeTextCommandValidator()
        {
            RuleFor(x => x.PurchaseId)
                .NotEmpty()
                .WithMessage("PurchaseId is required.");
        }
    }
}