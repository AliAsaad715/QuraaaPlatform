using MediatR;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Libraries.Commands.IssueLibraryRegistrationLink
{
    public sealed record IssueLibraryRegistrationLinkCommand(
        Guid UserId,
        Guid AuthenticationSessionId)
        : IRequest<AppResult<LibraryRegistrationLinkResponse>>;
}
