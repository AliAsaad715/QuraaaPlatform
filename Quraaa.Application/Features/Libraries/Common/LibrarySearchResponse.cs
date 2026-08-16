namespace Quraaa.Application.Features.Libraries.Common
{
    public record LibrarySearchResponse(
        Guid Id,
        string Name,
        string? LogoUrl,
        string? Location,
        int TotalActiveListingsCount);
}
