using MediatR;
using Quraaa.Application.Features.Profiles.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Profiles.Queries.GetMyProfile
{
    public record GetMyProfileQuery(Guid UserId) : IRequest<AppResult<ProfileResponse>>;
}
