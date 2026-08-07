using Quraaa.Application.Shared.Files;

namespace Quraaa.API.Services
{
    public sealed class FileAccessService : IFileAccessService
    {
        private readonly IFileStorageService _fileStorageService;

        public FileAccessService(IFileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }

        public bool TryPrepareDownload(
            string relativePath,
            string downloadFileNameStem,
            out DigitalAssetFileDescriptor descriptor)
        {
            descriptor = null!;

            if (!_fileStorageService.TryGetPhysicalPath(relativePath, out var physicalPath))
                return false;

            // Re-stat rather than trust TryGetPhysicalPath's existence check: that check
            // already happened once, and the file could be deleted concurrently between
            // the two calls.
            var fileInfo = new FileInfo(physicalPath);
            if (!fileInfo.Exists)
                return false;

            var extension = Path.GetExtension(physicalPath);
            var fileName = SanitizeFileName(downloadFileNameStem) + extension;

            descriptor = new DigitalAssetFileDescriptor(
                PhysicalPath: physicalPath,
                DownloadFileName: fileName,
                ContentType: ResolveContentType(extension),
                ContentLength: fileInfo.Length,
                ETag: ComputeETag(fileInfo),
                LastModifiedUtc: new DateTimeOffset(fileInfo.LastWriteTimeUtc, TimeSpan.Zero));
            return true;
        }

        // LastWriteTime + Length is enough to detect any content change and is
        // effectively free, unlike hashing the full file on every request — these
        // digital assets can be up to 100 MB (see BulkUploadBooksCommandValidator).
        private static string ComputeETag(FileInfo fileInfo) =>
            $"\"{fileInfo.LastWriteTimeUtc.Ticks:x}-{fileInfo.Length:x}\"";

        private static string ResolveContentType(string extension) => extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };

        private static string SanitizeFileName(string stem)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var cleaned = new string(stem.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray()).Trim();

            return string.IsNullOrWhiteSpace(cleaned) ? "download" : cleaned;
        }
    }
}
