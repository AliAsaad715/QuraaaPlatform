namespace Quraaa.Application.Shared.Files
{
    public static class ImageFileSignature
    {
        private const int HeaderLength = 12;

        public static bool MatchesDeclaredExtension(IUploadedFile? file)
        {
            if (file is null || file.Length <= 0)
            {
                return false;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            try
            {
                using var stream = file.OpenReadStream();
                Span<byte> header = stackalloc byte[HeaderLength];
                var bytesRead = 0;

                while (bytesRead < header.Length)
                {
                    var read = stream.Read(header[bytesRead..]);
                    if (read == 0)
                    {
                        break;
                    }

                    bytesRead += read;
                }

                return extension switch
                {
                    ".jpg" or ".jpeg" =>
                        bytesRead >= 3 &&
                        header[0] == 0xFF &&
                        header[1] == 0xD8 &&
                        header[2] == 0xFF,

                    ".png" =>
                        bytesRead >= 8 &&
                        header[..8].SequenceEqual(
                            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),

                    ".webp" =>
                        bytesRead >= HeaderLength &&
                        header[..4].SequenceEqual("RIFF"u8) &&
                        header[8..12].SequenceEqual("WEBP"u8),

                    _ => false
                };
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
