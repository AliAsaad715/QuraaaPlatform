using Quraaa.Application.Shared.Files;

namespace Quraaa.Application.Features.Orders.Common
{
    public record DigitalOrderItemDownloadResponse(
        Guid OrderId,
        Guid OrderItemId,
        DigitalAssetFileDescriptor File);
}
