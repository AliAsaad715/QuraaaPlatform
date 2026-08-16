using System.Net;

namespace Quraaa.API.IntegrationTests
{
    public sealed class RemovedOrderDownloadEndpointTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public RemovedOrderDownloadEndpointTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetOrderItemDownload_ReturnsNotFound_BecauseRouteIsRemoved()
        {
            var response = await _client.GetAsync(
                $"/api/orders/{Guid.NewGuid()}/items/{Guid.NewGuid()}/download");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
