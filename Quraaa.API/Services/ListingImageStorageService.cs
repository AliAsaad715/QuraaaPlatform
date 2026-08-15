using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Files;

namespace Quraaa.API.Services
{
    public class ListingImageStorageService : IListingImageStorageService
    {
        private readonly IImageStorageService _imageStorageService;

        public ListingImageStorageService(IImageStorageService imageStorageService)
        {
            _imageStorageService = imageStorageService;
        }

        public Task<string> SaveCoverImageAsync(
            IUploadedFile file,
            CancellationToken cancellationToken = default) =>
            _imageStorageService.UploadAsync(
                file,
                ImageAssetKind.ListingCover,
                cancellationToken);

        public Task DeleteAsync(
            string? storedUrl,
            CancellationToken cancellationToken = default) =>
            _imageStorageService.DeleteAsync(storedUrl, cancellationToken);
    }
}
