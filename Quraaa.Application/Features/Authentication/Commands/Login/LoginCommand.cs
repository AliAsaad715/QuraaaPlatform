using MediatR;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Authentication.Commands.Login
{
    public record LoginCommand
    (
        string PhoneNumber,
        string Password
    ) : IRequest<AppResult<AuthResponse>>;
}
