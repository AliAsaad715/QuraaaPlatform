namespace Quraaa.Application.Shared.Files
{
    public interface IUploadedFile
    {
        string FileName { get; }
        string ContentType { get; }
        long Length { get; }

        Task CopyToAsync(Stream target, CancellationToken cancellationToken = default);
    }
}
