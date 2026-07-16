using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Carts.Commands.ProcessStripeWebhook
{
    public record ProcessStripeWebhookCommand(string Payload, string SignatureHeader) : IRequest<AppResult>;
}
