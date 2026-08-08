using Microsoft.AspNetCore.Mvc;

namespace Quraaa.API.Requests.Listings
{
    public record AddDigitalBookRequest(
        [FromForm] decimal Price,
        [FromForm] string Isbn,
        [FromForm] IFormFile DigitalAsset
    );
}