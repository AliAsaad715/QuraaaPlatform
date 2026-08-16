using System.Net;
using System.Net.Http.Json;
using Quraaa.Application.Features.Listings.Commands.AddPhysicalBook;
using Quraaa.Application.Features.Listings.Queries.ValidateIsbn;

namespace Quraaa.API.IntegrationTests
{
    /// <summary>
    /// End-to-end tests for GET /api/books/validate-isbn/{isbn}: real HTTP request through
    /// the actual ASP.NET Core pipeline, routing, FluentValidation, and MediatR handler,
    /// with only the outbound Google Books call faked (see CustomWebApplicationFactory).
    /// </summary>
    public class ValidateIsbnEndpointTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ValidateIsbnEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ValidateIsbn_ReturnsBookMetadata_WhenGoogleBooksHasAMatch()
        {
            const string isbn = "9780132350884";
            _factory.BookMetadataService.SetResult(isbn, new BookMetadataDto(
                Title: "Clean Code",
                Authors: "Robert C. Martin",
                Description: "A handbook of agile software craftsmanship.",
                ThumbnailUrl: "https://example.com/clean-code.jpg",
                Publisher: "Prentice Hall",
                PublishedDate: "2008",
                Language: "en",
                PageCount: 464));

            var response = await _client.GetAsync($"/api/books/validate-isbn/{isbn}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<IsbnLookupResponse>();
            Assert.NotNull(body);
            Assert.Equal(isbn, body!.Isbn);
            Assert.Equal("Clean Code", body.Title);
            Assert.Equal("Robert C. Martin", body.Author);
            Assert.Equal("Prentice Hall", body.Publisher);
            Assert.Equal(464, body.PageCount);
        }

        [Fact]
        public async Task ValidateIsbn_ReturnsNotFound_WhenGoogleBooksHasNoMatch()
        {
            const string isbn = "0000000000000";
            _factory.BookMetadataService.SetResult(isbn, null);

            var response = await _client.GetAsync($"/api/books/validate-isbn/{isbn}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ValidateIsbn_ReturnsBadRequest_WhenIsbnIsBlank()
        {
            var response = await _client.GetAsync("/api/books/validate-isbn/%20");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
