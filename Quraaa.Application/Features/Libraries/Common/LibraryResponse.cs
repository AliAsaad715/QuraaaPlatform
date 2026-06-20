using Quraaa.Domain.Library.Enums;

namespace Quraaa.Application.Features.Libraries.Common
{
    public record UserLibraryResponse(
        Guid Id,
        string LibraryName,
        string Location,
        string LibraryImage,
        string HeaderImage,
        string Email,
        LibraryApprovalStatus ApprovalStatus
    );
}
