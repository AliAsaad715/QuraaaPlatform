using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Authentication.Commands.ForgotPassword
{
    public record ForgotPasswordCommand(
        string PhoneNumber,
        string ClientIp
    ) : IRequest<AppResult>;
}
