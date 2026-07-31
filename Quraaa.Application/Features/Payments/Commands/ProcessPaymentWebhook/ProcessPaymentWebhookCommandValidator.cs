using FluentValidation;

namespace Quraaa.Application.Features.Payments.Commands.ProcessPaymentWebhook
{
    public class ProcessPaymentWebhookCommandValidator : AbstractValidator<ProcessPaymentWebhookCommand>
    {
        public ProcessPaymentWebhookCommandValidator()
        {
            RuleFor(x => x.Payload).NotEmpty();
            RuleFor(x => x.SignatureHeader).NotEmpty();
        }
    }
}
