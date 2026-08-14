using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Files;

namespace Quraaa.API.Services
{
    public class LibraryImageStorageService : ILibraryImageStorageService
    {
        private readonly IImageStorageService _imageStorageService;

        public LibraryImageStorageService(IImageStorageService imageStorageService)
        {
            _imageStorageService = imageStorageService;
        }

        public Task<string> SaveLibraryImageAsync(
            IUploadedFile file,
            CancellationToken cancellationToken = default) =>
            _imageStorageService.UploadAsync(
                file,
                ImageAssetKind.LibraryLogo,
                cancellationToken);

        public Task<string> SaveHeaderImageAsync(
            IUploadedFile file,
            CancellationToken cancellationToken = default) =>
            _imageStorageService.UploadAsync(
                file,
                ImageAssetKind.LibraryHeader,
                cancellationToken);

        public Task DeleteAsync(
            string? storedPath,
            CancellationToken cancellationToken = default) =>
            _imageStorageService.DeleteAsync(storedPath, cancellationToken);
    }
}
