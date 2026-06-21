using Microsoft.Extensions.Configuration;
using Quraaa.Application.Features.Libraries.Commands.AddPhysicalBook;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Infrastructure.Models;
using System.Net.Http.Json;

namespace Quraaa.Infrastructure.Services
{
    public class GoogleBooksService : IBookMetadataService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GoogleBooksService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GoogleBooks__ApiKey"] ?? string.Empty;
        }

        public async Task<BookMetadataDto?> GetBookByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
        {
            var cleanIsbn = isbn.Replace("-", "").Trim();
            var endpoint = $"books/v1/volumes?q=isbn:{cleanIsbn}&key={_apiKey}";

            try
            {
                var response = await _httpClient.GetFromJsonAsync<GoogleBooksResponse>(endpoint, cancellationToken);

                if (response?.Items == null || !response.Items.Any())
                {
                    return null;
                }

                var volumeInfo = response.Items.First().VolumeInfo;

                return new BookMetadataDto(
                    Title: volumeInfo.Title ?? string.Empty,
                    Authors: volumeInfo.Authors != null ? string.Join(", ", volumeInfo.Authors) : string.Empty,
                    Description: volumeInfo.Description ?? string.Empty,
                    ThumbnailUrl: volumeInfo.ImageLinks?.Thumbnail?.Replace("http://", "https://") ?? string.Empty,
                    Publisher: volumeInfo.Publisher ?? string.Empty,
                    PublishedDate: volumeInfo.PublishedDate ?? string.Empty
                );
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
