using Microsoft.Extensions.Configuration;

namespace Quraaa.Application.Shared.Services
{
    public class ImageUrlFormatter : IImageUrlFormatter
    {
        private readonly string _baseUrl;

        public ImageUrlFormatter(IConfiguration configuration)
        {
            _baseUrl = configuration["BaseAPIURL"]?.TrimEnd('/') ?? string.Empty;
        }

        public string Format(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return string.Empty;
            }

            if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return imagePath;
            }

            var cleanPath = imagePath.Replace("\\", "/").TrimStart('/');
            return $"{_baseUrl}/{cleanPath}";
        }
    }
}
