using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Files;

namespace Quraaa.Infrastructure.Services
{
    public sealed class CloudinaryImageStorageService : IImageStorageService
    {
        private const long MaxImageSizeInBytes = 5L * 1024 * 1024;
        private const string OwnedPublicIdPrefix = "quraa/";

        private static readonly IReadOnlySet<string> AllowedExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

        private static readonly IReadOnlySet<string> AllowedContentTypes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/jpg",
                "image/png",
                "image/webp"
            };

        private readonly Cloudinary _cloudinary;
        private readonly CloudinaryOptions _options;
        private readonly ILogger<CloudinaryImageStorageService> _logger;

        public CloudinaryImageStorageService(
            IOptions<CloudinaryOptions> options,
            ILogger<CloudinaryImageStorageService> logger)
        {
            _options = options.Value;
            _logger = logger;
            _cloudinary = new Cloudinary(new Account(
                _options.CloudName,
                _options.ApiKey,
                _options.ApiSecret));
            _cloudinary.Api.Secure = true;
        }

        public async Task<string> UploadAsync(
            IUploadedFile file,
            ImageAssetKind assetKind,
            CancellationToken cancellationToken = default)
        {
            ValidateImage(file);

            var assetName = Guid.NewGuid().ToString("N");
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var publicId = $"{GetFolder(assetKind)}/{assetName}";

            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription($"{assetName}{extension}", stream),
                PublicId = publicId,
                Overwrite = false
            };

            try
            {
                var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

                if (result.Error is not null)
                {
                    throw new ImageStorageException("Cloudinary rejected the image upload.");
                }

                if (!string.Equals(result.PublicId, publicId, StringComparison.Ordinal)
                    || result.SecureUrl is null
                    || result.SecureUrl.Scheme != Uri.UriSchemeHttps)
                {
                    throw new ImageStorageException(
                        "Cloudinary returned an invalid image upload response.");
                }

                return result.SecureUrl.AbsoluteUri;
            }
            catch (OperationCanceledException)
            {
                await TryDeleteByPublicIdAsync(publicId, CancellationToken.None);
                throw;
            }
            catch (ImageStorageException exception)
            {
                await TryDeleteByPublicIdAsync(publicId, CancellationToken.None);
                _logger.LogError(
                    exception,
                    "Cloudinary image upload failed for asset kind {AssetKind}.",
                    assetKind);
                throw;
            }
            catch (Exception exception)
            {
                await TryDeleteByPublicIdAsync(publicId, CancellationToken.None);
                _logger.LogError(
                    exception,
                    "Cloudinary image upload failed for asset kind {AssetKind}.",
                    assetKind);
                throw new ImageStorageException(
                    "The image could not be stored by the external image provider.",
                    exception);
            }
        }

        public async Task DeleteAsync(
            string? storedUrl,
            CancellationToken cancellationToken = default)
        {
            if (!TryExtractOwnedPublicId(storedUrl, out var publicId))
            {
                // This intentionally covers old /uploads paths and third-party URLs.
                // They are not Cloudinary assets owned by this application.
                return;
            }

            await TryDeleteByPublicIdAsync(publicId, cancellationToken);
        }

        private async Task TryDeleteByPublicIdAsync(
            string publicId,
            CancellationToken cancellationToken)
        {
            try
            {
                var deletionParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image,
                    Type = "upload",
                    Invalidate = true
                };

                cancellationToken.ThrowIfCancellationRequested();
                var result = await _cloudinary.DestroyAsync(deletionParams);

                if (result.Error is not null
                    || (!string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(result.Result, "not found", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning(
                        "Cloudinary did not confirm deletion for image {PublicId}. Result: {Result}.",
                        publicId,
                        result.Result);
                }
            }
            catch (Exception exception)
            {
                // External cleanup must never corrupt or roll back already-valid
                // database state. Operators still get enough context to retry it.
                _logger.LogWarning(
                    exception,
                    "Could not delete Cloudinary image {PublicId}.",
                    publicId);
            }
        }

        private bool TryExtractOwnedPublicId(
            string? storedUrl,
            out string publicId)
        {
            publicId = string.Empty;

            if (!Uri.TryCreate(storedUrl, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || !string.Equals(uri.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                var segments = uri.AbsolutePath
                    .Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Uri.UnescapeDataString)
                    .ToArray();

                if (segments.Length < 5
                    || !string.Equals(segments[0], _options.CloudName, StringComparison.Ordinal)
                    || !string.Equals(segments[1], "image", StringComparison.Ordinal)
                    || !string.Equals(segments[2], "upload", StringComparison.Ordinal))
                {
                    return false;
                }

                var publicIdStart = segments[3].Length > 1
                    && segments[3][0] == 'v'
                    && long.TryParse(segments[3][1..], out _)
                        ? 4
                        : 3;

                if (publicIdStart >= segments.Length)
                {
                    return false;
                }

                var publicIdSegments = segments[publicIdStart..];
                var lastSegment = publicIdSegments[^1];
                var extensionIndex = lastSegment.LastIndexOf('.');
                if (extensionIndex <= 0)
                {
                    return false;
                }

                publicIdSegments[^1] = lastSegment[..extensionIndex];
                var candidate = string.Join('/', publicIdSegments);

                if (!candidate.StartsWith(OwnedPublicIdPrefix, StringComparison.Ordinal)
                    || candidate.Contains("..", StringComparison.Ordinal))
                {
                    return false;
                }

                publicId = candidate;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void ValidateImage(IUploadedFile file)
        {
            ArgumentNullException.ThrowIfNull(file);

            var extension = Path.GetExtension(file.FileName);
            if (file.Length <= 0
                || file.Length > MaxImageSizeInBytes
                || !AllowedExtensions.Contains(extension)
                || !AllowedContentTypes.Contains(file.ContentType)
                || !ImageFileSignature.MatchesDeclaredExtension(file))
            {
                throw new ArgumentException(
                    "The uploaded file is not a valid supported image.",
                    nameof(file));
            }
        }

        private static string GetFolder(ImageAssetKind assetKind) =>
            assetKind switch
            {
                ImageAssetKind.LibraryLogo => "quraa/libraries/logos",
                ImageAssetKind.LibraryHeader => "quraa/libraries/headers",
                ImageAssetKind.BookCover => "quraa/books/covers",
                ImageAssetKind.ListingCover => "quraa/listings/covers",
                _ => throw new ArgumentOutOfRangeException(nameof(assetKind), assetKind, null)
            };
    }
}
