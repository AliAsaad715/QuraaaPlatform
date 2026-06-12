using MediatR;
using Quraaa.Application.Features.Profiles.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.User.Enums;

namespace Quraaa.Application.Features.Profiles.Commands.UpdateProfile
{
    public record UpdateProfileCommand(
        Guid UserId,
        string FirstName,
        string LastName,
        Gender Gender,
        DateOnly DateOfBirth,
        string? ProfileImageUrl,
        List<string> Interests
    ) : IRequest<AppResult<ProfileResponse>>;
}
