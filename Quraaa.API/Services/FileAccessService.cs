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

            var extension = Path.GetExtension(physicalPath);
            var fileName = SanitizeFileName(downloadFileNameStem) + extension;

            descriptor = new DigitalAssetFileDescriptor(physicalPath, fileName, ResolveContentType(extension));
            return true;
        }

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
