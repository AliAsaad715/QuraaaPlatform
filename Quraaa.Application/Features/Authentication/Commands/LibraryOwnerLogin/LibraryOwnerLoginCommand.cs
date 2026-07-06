using MediatR;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Authentication.Commands.LibraryOwnerLogin
{
    public record LibraryOwnerLoginCommand(
        string Email,
        string Password,
        string ClientIp
    ) : IRequest<AppResult<AuthResponse>>;
}
