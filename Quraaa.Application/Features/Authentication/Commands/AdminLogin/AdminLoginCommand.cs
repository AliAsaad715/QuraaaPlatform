using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Authentication.Commands.AdminLogin
{
    public record AdminLoginCommand(
        string PhoneNumber,
        string Password,
        string ClientIp
    ) : IRequest<AppResult>;
}
