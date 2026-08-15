using Microsoft.AspNetCore.Mvc;

namespace Quraaa.API.Requests.Listings
{
    public record UpdateListingDigitalAssetRequest(
        [FromForm] IFormFile DigitalAsset
    );
}
