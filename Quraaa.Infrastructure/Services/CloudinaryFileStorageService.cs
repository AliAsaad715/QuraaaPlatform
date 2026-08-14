using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Files;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;

namespace Quraaa.Infrastructure.Services
{
    /// <summary>
    /// Stores newly uploaded book documents as authenticated Cloudinary raw assets.
    /// Legacy local book paths remain readable/deletable for existing database rows
    /// and the packaged seed ebook, but this service never writes new files locally.
    /// </summary>
    public sealed class CloudinaryFileStorageService : IFileStorageService
    {
        private const long MaxPdfSizeInBytes = 100L * 1024 * 1024;
        private const long MaxWordSizeInBytes = 50L * 1024 * 1024;
        private const int BufferSize = 81_920;
        private const string DeliveryType = "authenticated";
        private const string OwnedPublicIdPrefix = "quraa/books/files/";
        private const string CloudReferencePrefix = "cloudinary://raw/authenticated/";

        private static readonly IReadOnlySet<string> AllowedExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf",
                ".doc",
                ".docx"
            };

        private readonly Cloudinary _cloudinary;
        private readonly CloudinaryOptions _options;
        private readonly HttpClient _httpClient;
        private readonly ILogger<CloudinaryFileStorageService> _logger;
        private readonly string _legacyRootPath;
        private readonly string _legacyBooksRootPath;

        public CloudinaryFileStorageService(
            IHostEnvironment environment,
            IOptions<CloudinaryOptions> cloudinaryOptions,
            IOptions<FileStorageOptions> fileStorageOptions,
            HttpClient httpClient,
            ILogger<CloudinaryFileStorageService> logger)
        {
            _options = cloudinaryOptions.Value;
            _httpClient = httpClient;
            _logger = logger;

            _cloudinary = new Cloudinary(new Account(
                _options.CloudName,
                _options.ApiKey,
                _options.ApiSecret));

            // Cloudinary creates its own HttpClient by default. Replace it so every
            // SDK operation uses HttpClientFactory's pooled handler and timeout.
            var sdkOwnedClient = _cloudinary.Api.Client;
            _cloudinary.Api.Client = httpClient;
            sdkOwnedClient.Dispose();

            _cloudinary.Api.Secure = true;

            var configuredRoot = fileStorageOptions.Value.RootPath;
            var resolvedRoot = Path.IsPathRooted(configuredRoot)
                ? configuredRoot
                : Path.Combine(environment.ContentRootPath, configuredRoot);

            _legacyRootPath = Path.GetFullPath(resolvedRoot);
            _legacyBooksRootPath = Path.GetFullPath(Path.Combine(_legacyRootPath, "books"));
        }

        public async Task<string> SaveAsync(
            IUploadedFile file,
            string subFolder,
            CancellationToken cancellationToken = default)
        {
            var extension = ValidateDocument(file, subFolder);
            var assetName = Guid.NewGuid().ToString("N");
            var publicId = $"{GetCloudinaryFolder(extension)}/{assetName}{extension}";

            await using var stream = file.OpenReadStream();
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription($"{assetName}{extension}", stream),
                PublicId = publicId,
                Type = DeliveryType,
                Overwrite = false,
                DiscardOriginalFilename = true
            };

            try
            {
                var result = await _cloudinary.UploadAsync(
                    uploadParams,
                    "raw",
                    cancellationToken);

                if (result.Error is not null)
                {
                    throw new FileStorageException("Cloudinary rejected the private file upload.");
                }

                if (!string.Equals(result.PublicId, publicId, StringComparison.Ordinal)
                    || !string.Equals(result.ResourceType, "raw", StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(result.Type, DeliveryType, StringComparison.OrdinalIgnoreCase)
                    || result.SecureUrl is null
                    || result.SecureUrl.Scheme != Uri.UriSchemeHttps)
                {
                    throw new FileStorageException(
                        "Cloudinary returned an invalid private file upload response.");
                }

                return BuildStoredReference(publicId);
            }
            catch (OperationCanceledException)
            {
                await TryDeleteCloudAssetAsync(publicId, throwOnFailure: false, CancellationToken.None);
                throw;
            }
            catch (FileStorageException)
            {
                await TryDeleteCloudAssetAsync(publicId, throwOnFailure: false, CancellationToken.None);
                throw;
            }
            catch (Exception exception)
            {
                await TryDeleteCloudAssetAsync(publicId, throwOnFailure: false, CancellationToken.None);
                throw new FileStorageException(
                    "The private file could not be stored by Cloudinary.",
                    exception);
            }
        }

        public async Task<Stream?> OpenReadAsync(
            string storedReference,
            CancellationToken cancellationToken = default)
        {
            if (TryExtractOwnedPublicId(storedReference, out var publicId))
            {
                return await DownloadToTemporarySeekableStreamAsync(
                    publicId,
                    cancellationToken);
            }

            if (!TryResolveLegacyBookPath(storedReference, requireExists: true, out var physicalPath))
                return null;

            return new FileStream(
                physicalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
        }

        public Task<StoredFileDownloadSource?> GetDownloadSourceAsync(
            string storedReference,
            string downloadFileName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (TryExtractOwnedPublicId(storedReference, out var publicId))
            {
                var extension = Path.GetExtension(publicId).ToLowerInvariant();
                var downloadUri = CreatePrivateDownloadUri(publicId, downloadFileName);

                return Task.FromResult<StoredFileDownloadSource?>(new StoredFileDownloadSource(
                    extension,
                    PhysicalPath: null,
                    RemoteDownloadUri: downloadUri,
                    ContentLength: null,
                    ETag: null,
                    LastModifiedUtc: null));
            }

            if (!TryResolveLegacyBookPath(storedReference, requireExists: true, out var physicalPath))
                return Task.FromResult<StoredFileDownloadSource?>(null);

            var fileInfo = new FileInfo(physicalPath);
            if (!fileInfo.Exists)
                return Task.FromResult<StoredFileDownloadSource?>(null);

            var source = new StoredFileDownloadSource(
                Path.GetExtension(physicalPath).ToLowerInvariant(),
                PhysicalPath: physicalPath,
                RemoteDownloadUri: null,
                ContentLength: fileInfo.Length,
                ETag: ComputeETag(fileInfo),
                LastModifiedUtc: new DateTimeOffset(fileInfo.LastWriteTimeUtc, TimeSpan.Zero));

            return Task.FromResult<StoredFileDownloadSource?>(source);
        }

        public async Task DeleteAsync(
            string storedReference,
            CancellationToken cancellationToken = default)
        {
            if (TryExtractOwnedPublicId(storedReference, out var publicId))
            {
                await TryDeleteCloudAssetAsync(publicId, throwOnFailure: true, cancellationToken);
                return;
            }

            if (TryResolveLegacyBookPath(storedReference, requireExists: false, out var physicalPath)
                && File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }

        public async IAsyncEnumerable<StoredFileEntry> EnumerateFilesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var entry in EnumerateCloudFilesAsync(cancellationToken))
                yield return entry;

            await foreach (var entry in EnumerateLegacyFilesAsync(cancellationToken))
                yield return entry;
        }

        private async IAsyncEnumerable<StoredFileEntry> EnumerateCloudFilesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            string? nextCursor = null;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                var parameters = new ListResourcesByPrefixParams
                {
                    ResourceType = ResourceType.Raw,
                    Type = DeliveryType,
                    Prefix = OwnedPublicIdPrefix,
                    MaxResults = 500,
                    NextCursor = nextCursor
                };

                ListResourcesResult result;
                try
                {
                    result = await _cloudinary.ListResourcesAsync(parameters, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw new FileStorageException(
                        "Cloudinary private files could not be enumerated.",
                        exception);
                }

                if (result.Error is not null)
                {
                    throw new FileStorageException(
                        "Cloudinary rejected private file enumeration.");
                }

                foreach (var resource in result.Resources ?? [])
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!string.Equals(resource.ResourceType, "raw", StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(resource.Type, DeliveryType, StringComparison.OrdinalIgnoreCase)
                        || !IsValidOwnedPublicId(resource.PublicId))
                    {
                        continue;
                    }

                    var createdAtUtc = TryParseCloudinaryCreatedAt(resource.CreatedAt)
                        ?? DateTime.UtcNow;

                    yield return new StoredFileEntry(
                        BuildStoredReference(resource.PublicId),
                        createdAtUtc,
                        resource.Bytes);
                }

                nextCursor = string.IsNullOrWhiteSpace(result.NextCursor)
                    ? null
                    : result.NextCursor;
            }
            while (nextCursor is not null);
        }

        private async IAsyncEnumerable<StoredFileEntry> EnumerateLegacyFilesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (!Directory.Exists(_legacyBooksRootPath))
                yield break;

            var rootPrefixLength = _legacyRootPath.Length + 1;
            var processed = 0;

            foreach (var filePath in Directory.EnumerateFiles(
                         _legacyBooksRootPath,
                         "*",
                         SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var extension = Path.GetExtension(filePath);
                if (!AllowedExtensions.Contains(extension))
                    continue;

                var info = new FileInfo(filePath);
                var relativePath = filePath[rootPrefixLength..]
                    .Replace(Path.DirectorySeparatorChar, '/');

                yield return new StoredFileEntry(
                    relativePath,
                    info.LastWriteTimeUtc,
                    info.Length);

                if (++processed % 256 == 0)
                    await Task.Yield();
            }
        }

        private async Task<Stream?> DownloadToTemporarySeekableStreamAsync(
            string publicId,
            CancellationToken cancellationToken)
        {
            var downloadUri = CreatePrivateDownloadUri(
                publicId,
                Path.GetFileName(publicId));

            using var response = await _httpClient.GetAsync(
                downloadUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.NotFound
                or HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new FileStorageException(
                    $"Cloudinary returned HTTP {(int)response.StatusCode} while reading a private file.");
            }

            var extension = Path.GetExtension(publicId).ToLowerInvariant();
            var maximumBytes = GetMaximumBytes(extension);

            if (response.Content.Headers.ContentLength is long contentLength
                && contentLength > maximumBytes)
            {
                throw new FileStorageException(
                    "The stored private file exceeds the configured size limit.");
            }

            var temporaryDirectory = Path.Combine(Path.GetTempPath(), "quraa-private-files");
            Directory.CreateDirectory(temporaryDirectory);
            var temporaryPath = Path.Combine(
                temporaryDirectory,
                $"{Guid.NewGuid():N}{extension}");

            var temporaryStream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.Read,
                BufferSize,
                FileOptions.Asynchronous
                    | FileOptions.SequentialScan
                    | FileOptions.DeleteOnClose);

            try
            {
                await using var remoteStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await CopyWithLimitAsync(
                    remoteStream,
                    temporaryStream,
                    maximumBytes,
                    cancellationToken);
                temporaryStream.Position = 0;
                return temporaryStream;
            }
            catch
            {
                await temporaryStream.DisposeAsync();
                throw;
            }
        }

        private Uri CreatePrivateDownloadUri(string publicId, string downloadFileName)
        {
            var expiresAt = DateTimeOffset.UtcNow
                .AddSeconds(_options.PrivateDownloadUrlLifetimeSeconds)
                .ToUnixTimeSeconds();

            var url = _cloudinary.DownloadPrivate(
                publicId,
                attachment: false,
                format: null,
                type: DeliveryType,
                expiresAt: expiresAt,
                resourceType: "raw",
                transformation: null,
                targetFilename: downloadFileName);

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || !string.Equals(uri.Host, "api.cloudinary.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new FileStorageException(
                    "Cloudinary generated an invalid private download URL.");
            }

            return uri;
        }

        private async Task TryDeleteCloudAssetAsync(
            string publicId,
            bool throwOnFailure,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Raw,
                    Type = DeliveryType,
                    Invalidate = true
                });

                if (result.Error is null
                    && (string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(result.Result, "not found", StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                if (throwOnFailure)
                {
                    throw new FileStorageException(
                        "Cloudinary did not confirm private file deletion.");
                }

                _logger.LogWarning(
                    "Cloudinary did not confirm cleanup of private file {PublicId}. Result: {Result}.",
                    publicId,
                    result.Result);
            }
            catch (OperationCanceledException) when (throwOnFailure)
            {
                throw;
            }
            catch (FileStorageException) when (throwOnFailure)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (throwOnFailure)
                {
                    throw new FileStorageException(
                        "The private file could not be deleted from Cloudinary.",
                        exception);
                }

                _logger.LogWarning(
                    exception,
                    "Could not clean up Cloudinary private file {PublicId}.",
                    publicId);
            }
        }

        private bool TryExtractOwnedPublicId(string? storedReference, out string publicId)
        {
            publicId = string.Empty;

            if (string.IsNullOrWhiteSpace(storedReference))
                return false;

            if (storedReference.StartsWith(CloudReferencePrefix, StringComparison.Ordinal))
            {
                var candidate = storedReference[CloudReferencePrefix.Length..];
                if (!IsValidOwnedPublicId(candidate))
                    return false;

                publicId = candidate;
                return true;
            }

            // Tolerate an owned unsigned Cloudinary URL if an earlier deployment stored
            // SecureUrl directly instead of the application's opaque reference format.
            if (!Uri.TryCreate(storedReference, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || !string.Equals(uri.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrEmpty(uri.Query)
                || !string.IsNullOrEmpty(uri.Fragment))
            {
                return false;
            }

            try
            {
                var segments = uri.AbsolutePath
                    .Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Uri.UnescapeDataString)
                    .ToArray();

                if (segments.Length < 6
                    || !string.Equals(segments[0], _options.CloudName, StringComparison.Ordinal)
                    || !string.Equals(segments[1], "raw", StringComparison.Ordinal)
                    || !string.Equals(segments[2], DeliveryType, StringComparison.Ordinal))
                {
                    return false;
                }

                var publicIdStart = segments[3].Length > 1
                    && segments[3][0] == 'v'
                    && long.TryParse(segments[3][1..], out _)
                        ? 4
                        : 3;

                var candidate = string.Join('/', segments[publicIdStart..]);
                if (!IsValidOwnedPublicId(candidate))
                    return false;

                publicId = candidate;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool TryResolveLegacyBookPath(
            string? storedReference,
            bool requireExists,
            out string physicalPath)
        {
            physicalPath = string.Empty;

            if (string.IsNullOrWhiteSpace(storedReference)
                || Uri.TryCreate(storedReference, UriKind.Absolute, out _)
                || Path.IsPathRooted(storedReference))
            {
                return false;
            }

            var normalized = storedReference.Replace('\\', '/').TrimStart('/');
            if (normalized.StartsWith("uploads/books/", StringComparison.OrdinalIgnoreCase))
                normalized = $"books/{normalized["uploads/books/".Length..]}";

            if (!normalized.StartsWith("books/", StringComparison.OrdinalIgnoreCase)
                || !AllowedExtensions.Contains(Path.GetExtension(normalized)))
            {
                return false;
            }

            var segments = normalized.Split('/');
            if (segments.Any(segment =>
                    string.IsNullOrWhiteSpace(segment)
                    || segment is "." or ".."))
            {
                return false;
            }

            try
            {
                var combined = Path.GetFullPath(
                    Path.Combine(_legacyRootPath, Path.Combine(segments)));
                var rootPrefix = _legacyBooksRootPath.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                var comparison = OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;

                if (!combined.StartsWith(rootPrefix, comparison)
                    || (requireExists && !File.Exists(combined)))
                {
                    return false;
                }

                physicalPath = combined;
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException
                    or NotSupportedException
                    or PathTooLongException)
            {
                return false;
            }
        }

        private static string ValidateDocument(IUploadedFile file, string subFolder)
        {
            ArgumentNullException.ThrowIfNull(file);

            var normalizedFolder = subFolder.Replace('\\', '/').Trim('/');
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var folderMatchesExtension = normalizedFolder switch
            {
                "books" or "books/pdf" => extension == ".pdf",
                "books/docs" => extension is ".doc" or ".docx",
                _ => false
            };

            if (!folderMatchesExtension
                || file.Length <= 0
                || file.Length > GetMaximumBytes(extension)
                || !DocumentFileSignature.MatchesDeclaredExtension(file))
            {
                throw new ArgumentException(
                    "The uploaded file is not a valid supported book document.",
                    nameof(file));
            }

            return extension;
        }

        private static bool IsValidOwnedPublicId(string? candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate)
                || !candidate.StartsWith(OwnedPublicIdPrefix, StringComparison.Ordinal)
                || !AllowedExtensions.Contains(Path.GetExtension(candidate))
                || candidate.Contains('\\')
                || candidate.Any(character =>
                    !char.IsLetterOrDigit(character)
                    && character is not '/' and not '.' and not '-' and not '_'))
            {
                return false;
            }

            var segments = candidate.Split('/');
            return segments.All(segment =>
                !string.IsNullOrWhiteSpace(segment)
                && segment is not "." and not "..");
        }

        private static string BuildStoredReference(string publicId) =>
            $"{CloudReferencePrefix}{publicId}";

        private static string GetCloudinaryFolder(string extension) =>
            extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                ? $"{OwnedPublicIdPrefix}pdf".TrimEnd('/')
                : $"{OwnedPublicIdPrefix}docs".TrimEnd('/');

        private static long GetMaximumBytes(string extension) =>
            extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                ? MaxPdfSizeInBytes
                : extension is ".doc" or ".docx"
                    ? MaxWordSizeInBytes
                    : 0;

        private static string ComputeETag(FileInfo fileInfo) =>
            $"\"{fileInfo.LastWriteTimeUtc.Ticks:x}-{fileInfo.Length:x}\"";

        private static DateTime? TryParseCloudinaryCreatedAt(string? value) =>
            DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed)
                    ? parsed.UtcDateTime
                    : null;

        private static async Task CopyWithLimitAsync(
            Stream source,
            Stream destination,
            long maximumBytes,
            CancellationToken cancellationToken)
        {
            var buffer = new byte[BufferSize];
            long totalBytes = 0;

            while (true)
            {
                var bytesRead = await source.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0)
                    break;

                totalBytes += bytesRead;
                if (totalBytes > maximumBytes)
                {
                    throw new FileStorageException(
                        "The stored private file exceeds the configured size limit.");
                }

                await destination.WriteAsync(
                    buffer.AsMemory(0, bytesRead),
                    cancellationToken);
            }
        }
    }
}
