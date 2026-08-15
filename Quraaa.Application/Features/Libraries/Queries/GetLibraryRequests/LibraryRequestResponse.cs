using Quraaa.Domain.Library.Enums;

namespace Quraaa.Application.Features.Libraries.Queries.GetLibraryRequests
{
    public record LibraryRequestResponse(
        Guid LibraryId,
        string LibraryName,
        string Location,
        string LibraryImage,
        string HeaderImage,
        string Email,
        DateTime? EmailVerifiedAtUtc,
        LibraryApprovalStatus ApprovalStatus,
        decimal ProfitSharePercent,
        DateTime RequestedAt,
        RequesterInfo Requester
    );

    public record RequesterInfo(
        Guid UserId,
        string FirstName,
        string LastName,
        string PhoneNumber
    );
}
