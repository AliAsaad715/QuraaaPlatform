using System.IO.Compression;

namespace Quraaa.Application.Shared.Files
{
    public static class DocumentFileSignature
    {
        private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();
        private static readonly byte[] LegacyWordSignature =
            [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

        public static bool MatchesDeclaredExtension(IUploadedFile? file)
        {
            if (file is null || file.Length <= 0)
                return false;

            try
            {
                var extension = Path.GetExtension(file.FileName);
                return extension.ToLowerInvariant() switch
                {
                    ".pdf" => HasPrefix(file, PdfSignature),
                    ".doc" => HasPrefix(file, LegacyWordSignature),
                    ".docx" => IsDocxPackage(file),
                    _ => false
                };
            }
            catch (Exception exception) when (
                exception is IOException
                    or InvalidDataException
                    or NotSupportedException
                    or UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static bool HasPrefix(IUploadedFile file, byte[] signature)
        {
            using var stream = file.OpenReadStream();
            Span<byte> header = stackalloc byte[signature.Length];
            var bytesRead = 0;

            while (bytesRead < header.Length)
            {
                var read = stream.Read(header[bytesRead..]);
                if (read == 0)
                    break;

                bytesRead += read;
            }

            return bytesRead == signature.Length && header.SequenceEqual(signature);
        }

        private static bool IsDocxPackage(IUploadedFile file)
        {
            using var stream = file.OpenReadStream();
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

            // A DOCX is an Open Packaging Convention ZIP containing both the content
            // type manifest and the main Word document part. This rejects arbitrary ZIPs.
            return archive.GetEntry("[Content_Types].xml") is not null
                && archive.GetEntry("word/document.xml") is not null;
        }
    }
}
