namespace Quraaa.Application.Features.Libraries.Queries.GetLibraries
{
    public record PublicLibraryResponse(
        Guid Id,
        string LibraryName,
        string Location,
        string LibraryImage,
        string HeaderImage,
        string Email
    );
}