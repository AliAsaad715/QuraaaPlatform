using MediatR;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Libraries.Queries.GetMyProfile
{
    public record GetMyProfileQuery([property: JsonIgnore] Guid UserId) : IRequest<AppResult<MyProfileLibraryResponse>>;
}
