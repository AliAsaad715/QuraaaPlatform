namespace Quraaa.Application.Features.Orders.Common
{
    public record DigitalOrderItemDownloadResponse(
        Guid OrderId,
        Guid OrderItemId,
        string DigitalAssetPath);
}
