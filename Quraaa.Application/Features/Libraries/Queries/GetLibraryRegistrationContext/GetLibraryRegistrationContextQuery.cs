using MediatR;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Libraries.Queries.GetLibraryRegistrationContext
{
    public sealed record GetLibraryRegistrationContextQuery(string Token)
        : IRequest<AppResult<LibraryRegistrationContextResponse>>;
}
