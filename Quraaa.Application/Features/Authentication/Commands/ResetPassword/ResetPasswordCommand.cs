using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Authentication.Commands.ResetPassword
{
    public record ResetPasswordCommand(
        Guid UserId,
        string OldPassword,
        string NewPassword
    ) : IRequest<AppResult>;
}
