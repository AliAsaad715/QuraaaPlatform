namespace Quraaa.Application.Shared.Services
{
    /// <summary>
    /// Formats a stored relative path (or already-absolute URL) into the
    /// absolute URL clients should use, using the configured BaseAPIURL.
    /// </summary>
    public interface IImageUrlFormatter
    {
        string Format(string? imagePath);
    }
}
