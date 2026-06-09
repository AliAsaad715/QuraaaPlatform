using Quraaa.Application.Shared.Files;

namespace Quraaa.API.Requests.Files
{
    public sealed class FormFileUploadedFile : IUploadedFile
    {
        private readonly IFormFile _file;

        public FormFileUploadedFile(IFormFile file)
        {
            _file = file;
        }

        public string FileName => _file.FileName;
        public string ContentType => _file.ContentType;
        public long Length => _file.Length;

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            return _file.CopyToAsync(target, cancellationToken);
        }
    }
}
