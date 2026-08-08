using MediatR;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.Library.Enums;

namespace Quraaa.Application.Features.Libraries.Queries.GetLibraryRequests
{
    public record GetLibraryRequestsQuery : IRequest<AppResult<PagedResult<LibraryRequestResponse>>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public string? SearchTerm { get; init; }

        // Defaults to Pending — "get the requests" out of the box. Pass
        // ?status=Approved or ?status=Rejected through the same endpoint
        // for an audit view; pass nothing to omit the filter entirely and
        // see every library regardless of status.
        public LibraryApprovalStatus? Status { get; init; } = LibraryApprovalStatus.Pending;
    }
}