using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Authentication.Commands.ResetForgotPassword
{
    public record ResetForgotPasswordCommand(
        string PhoneNumber,
        string OtpCode,
        string NewPassword,
        string ClientIp
    ) : IRequest<AppResult>;
}
