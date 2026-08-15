using Microsoft.AspNetCore.Mvc;
using Quraaa.Domain.Marketplace.Enums;

namespace Quraaa.API.Requests.Listings
{
    public record AddUserPhysicalBookRequest(
        [FromForm] decimal Price,
        [FromForm] BookCondition Condition,
        [FromForm] string Isbn,
        [FromForm] IFormFile CoverImage
    );
}
