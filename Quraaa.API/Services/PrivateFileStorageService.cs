using Microsoft.Extensions.Options;
using Quraaa.Application.Shared.Files;
using System.Runtime.CompilerServices;

namespace Quraaa.API.Services
{
    /// <summary>
    /// Stores files under a root resolved against the host's content root (e.g.
    /// "{ContentRoot}/storage"), never wwwroot — so nothing saved here is reachable
    /// through <c>UseStaticFiles</c>, regardless of sub-folder.
    /// </summary>
    public sealed class PrivateFileStorageService : IFileStorageService
    {
        // 80 KB — matches OS cluster size for efficient sequential writes.
        private const int BufferSize = 81_920;

        private readonly string _rootPath;

        public PrivateFileStorageService(
            IWebHostEnvironment environment,
            IOptions<FileStorageOptions> options)
        {
            var configuredRoot = options.Value.RootPath;
            var resolvedRoot = Path.IsPathRooted(configuredRoot)
                ? configuredRoot
                : Path.Combine(environment.ContentRootPath, configuredRoot);

            _rootPath = Path.GetFullPath(resolvedRoot);
        }

        public async Task<string> SaveAsync(
            IUploadedFile file,
            string subFolder,
            CancellationToken cancellationToken = default)
        {
            var directory = Path.Combine(_rootPath, subFolder.Trim('/').Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(directory);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(directory, fileName);

            await using var stream = new FileStream(
                filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                BufferSize, useAsync: true);

            await file.CopyToAsync(stream, cancellationToken);

            return $"{subFolder.Trim('/')}/{fileName}";
        }

        public bool TryGetPhysicalPath(string relativePath, out string physicalPath)
        {
            physicalPath = string.Empty;

            if (!TryResolveWithinRoot(relativePath, out var candidatePath) || !File.Exists(candidatePath))
                return false;

            physicalPath = candidatePath;
            return true;
        }

        public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            if (TryResolveWithinRoot(relativePath, out var physicalPath) && File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }

            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<StoredFileEntry> EnumerateFilesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(_rootPath))
                yield break;

            var rootPrefixLength = _rootPath.Length + 1; // +1 strips the trailing separator
            var processed = 0;

            foreach (var filePath in Directory.EnumerateFiles(_rootPath, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var info = new FileInfo(filePath);
                var relativePath = filePath[rootPrefixLength..].Replace(Path.DirectorySeparatorChar, '/');

                yield return new StoredFileEntry(relativePath, info.LastWriteTimeUtc, info.Length);

                // Cooperative yield so walking a very large tree doesn't hold the thread throughout.
                if (++processed % 256 == 0)
                {
                    await Task.Yield();
                }
            }
        }

        // Rebuilds the path from individual segments so "..", ".", empty segments,
        // and rooted/absolute input are all rejected before the combined path is
        // canonicalized and re-checked against the root prefix.
        private bool TryResolveWithinRoot(string relativePath, out string physicalPath)
        {
            physicalPath = string.Empty;

            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
                return false;

            var segments = relativePath.Replace('\\', '/').Split('/');
            var cleanSegments = new List<string>(segments.Length);

            foreach (var segment in segments)
            {
                if (segment.Length == 0 || segment == ".")
                    continue;

                if (segment == "..")
                    return false;

                cleanSegments.Add(segment);
            }

            if (cleanSegments.Count == 0)
                return false;

            var combined = Path.GetFullPath(Path.Combine(_rootPath, Path.Combine(cleanSegments.ToArray())));

            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            var rootPrefix = _rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!combined.StartsWith(rootPrefix, comparison))
                return false;

            physicalPath = combined;
            return true;
        }
    }
}
