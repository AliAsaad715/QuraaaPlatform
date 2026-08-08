using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Payments.Commands.ProcessPaymentWebhook
{
    public record ProcessPaymentWebhookCommand(
        string Payload,
        string SignatureHeader)
        : IRequest<AppResult>;
}
