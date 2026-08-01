using MediatR;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Authentication.Commands.RefreshToken
{
    public record RefreshTokenCommand(
        string RefreshToken) : IRequest<AppResult<AuthResponse>>;
}
