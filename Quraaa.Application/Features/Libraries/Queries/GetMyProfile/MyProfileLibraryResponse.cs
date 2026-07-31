namespace Quraaa.Application.Features.Libraries.Queries.GetMyProfile
{
    public record MyProfileLibraryResponse(
        string LibraryName,
        string Location,
        string LibraryImage,
        string HeaderImage,
        string Email
    );
}
