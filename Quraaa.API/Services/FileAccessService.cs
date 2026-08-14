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

        public async Task<DigitalAssetFileDescriptor?> PrepareDownloadAsync(
            string storedReference,
            string downloadFileNameStem,
            CancellationToken cancellationToken = default)
        {
            var declaredExtension = Path.GetExtension(storedReference).ToLowerInvariant();
            if (!IsSupportedDocumentExtension(declaredExtension))
                return null;

            var fileName = SanitizeFileName(downloadFileNameStem) + declaredExtension;
            var source = await _fileStorageService.GetDownloadSourceAsync(
                storedReference,
                fileName,
                cancellationToken);

            if (source is null
                || !string.Equals(
                    source.FileExtension,
                    declaredExtension,
                    StringComparison.OrdinalIgnoreCase)
                || (source.PhysicalPath is null) == (source.RemoteDownloadUri is null))
            {
                return null;
            }

            return new DigitalAssetFileDescriptor(
                PhysicalPath: source.PhysicalPath,
                RemoteDownloadUri: source.RemoteDownloadUri,
                DownloadFileName: fileName,
                ContentType: ResolveContentType(declaredExtension),
                ContentLength: source.ContentLength,
                ETag: source.ETag,
                LastModifiedUtc: source.LastModifiedUtc);
        }

        private static bool IsSupportedDocumentExtension(string extension) =>
            extension is ".pdf" or ".doc" or ".docx";

        private static string ResolveContentType(string extension) => extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };

        private static string SanitizeFileName(string stem)
        {
            const string portableInvalidChars = "<>:\"/\\|?*";
            var invalidChars = Path.GetInvalidFileNameChars();
            var cleaned = new string(stem
                .Select(character =>
                    char.IsControl(character)
                    || portableInvalidChars.Contains(character)
                    || invalidChars.Contains(character)
                        ? '_'
                        : character)
                .ToArray())
                .Trim()
                .TrimEnd('.');

            return string.IsNullOrWhiteSpace(cleaned) ? "download" : cleaned;
        }
    }
}
