namespace Quraaa.Application.Shared.Files
{
    /// <summary>
    /// Stores publicly displayable images in durable external storage.
    /// Implementations return an absolute HTTPS delivery URL.
    /// </summary>
    public interface IImageStorageService
    {
        Task<string> UploadAsync(
            IUploadedFile file,
            ImageAssetKind assetKind,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Best-effort deletion for images owned by this application. Legacy local
        /// paths and URLs owned by other providers are ignored.
        /// </summary>
        Task DeleteAsync(
            string? storedUrl,
            CancellationToken cancellationToken = default);
    }
}
