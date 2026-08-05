using Microsoft.Extensions.Configuration;
using Quraaa.Application.Features.Listings.Commands.AddPhysicalBook;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Infrastructure.Models;
using System.Net.Http.Json;

namespace Quraaa.Infrastructure.Services
{
    public class OpenLibraryService : IBookMetadataService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public OpenLibraryService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<BookMetadataDto?> GetBookByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
        {
            var cleanIsbn = isbn.Replace("-", string.Empty).Trim();
            var endpoint = $"api/books?bibkeys=ISBN:{cleanIsbn}&format=json&jscmd=data";

            try
            {
                var response = await _httpClient.GetFromJsonAsync<Dictionary<string, OpenLibraryBookData>>(endpoint, cancellationToken);

                if (response is null || !response.TryGetValue($"ISBN:{cleanIsbn}", out var bookData) || bookData is null)
                {
                    return null;
                }

                return new BookMetadataDto(
                    Title: bookData.Title ?? string.Empty,
                    Authors: bookData.Authors is { Count: > 0 }
                        ? string.Join(", ", bookData.Authors.Select(author => author.Name).Where(name => !string.IsNullOrWhiteSpace(name)))
                        : string.Empty,
                    Description: bookData.Subtitle ?? string.Empty,
                    ThumbnailUrl: bookData.Cover?.Large ?? bookData.Cover?.Medium ?? bookData.Cover?.Small ?? string.Empty,
                    Publisher: bookData.Publishers is { Count: > 0 }
                        ? string.Join(", ", bookData.Publishers.Select(publisher => publisher.Name).Where(name => !string.IsNullOrWhiteSpace(name)))
                        : string.Empty,
                    PublishedDate: bookData.PublishDate ?? string.Empty,
                    Language: await ResolveLanguageAsync(cleanIsbn, cancellationToken)
                );
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<string> ResolveLanguageAsync(string isbn, CancellationToken cancellationToken)
        {
            var apiKey = _configuration["GoogleBooks:ApiKey"] ?? string.Empty;
            var endpoint = string.IsNullOrWhiteSpace(apiKey)
                ? $"https://www.googleapis.com/books/v1/volumes?q=isbn:{isbn}"
                : $"https://www.googleapis.com/books/v1/volumes?q=isbn:{isbn}&key={apiKey}";

            try
            {
                var response = await _httpClient.GetFromJsonAsync<GoogleBooksLanguageResponse>(endpoint, cancellationToken);

                return response?.Items?.FirstOrDefault()?.VolumeInfo?.Language?.Trim() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}