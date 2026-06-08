namespace Quraaa.Application.Features.Libraries.Common
{
    public record LibraryResponse(
        Guid Id,
        string LibraryName,
        string Location,
        string LibraryImage,
        string HeaderImage,
        string Email,
        Guid UserId
    );
}
