using Quraaa.Application.Shared.Files;

namespace Quraaa.Application.Features.Listings.Interfaces
{
    public interface IListingImageStorageService
    {
        /// <summary>Saves a seller-supplied listing cover/condition image and returns its absolute HTTPS delivery URL.</summary>
        Task<string> SaveCoverImageAsync(IUploadedFile file, CancellationToken cancellationToken = default);

        Task DeleteAsync(string? storedUrl, CancellationToken cancellationToken = default);
    }
}
