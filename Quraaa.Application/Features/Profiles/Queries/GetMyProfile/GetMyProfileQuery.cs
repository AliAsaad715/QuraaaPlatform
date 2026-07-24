using MediatR;
using Quraaa.Application.Features.Profiles.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Profiles.Queries.GetMyProfile
{
    public record GetMyProfileQuery([property: JsonIgnore] Guid UserId) : IRequest<AppResult<ProfileResponse>>;
}
