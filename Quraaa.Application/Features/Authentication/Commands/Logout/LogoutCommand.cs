using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Authentication.Commands.Logout
{
    public record LogoutCommand(
        string RefreshToken,
        string? AccessTokenId,
        DateTimeOffset? AccessTokenExpiresAt) : IRequest<AppResult>;
}
