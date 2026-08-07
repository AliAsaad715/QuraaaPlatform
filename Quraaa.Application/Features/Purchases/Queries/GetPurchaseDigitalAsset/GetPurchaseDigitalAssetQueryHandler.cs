using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Application.Shared.Files;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Purchases.Queries.GetPurchaseDigitalAsset
{
    public sealed class GetPurchaseDigitalAssetQueryHandler
        : BaseApplicationService<GetPurchaseDigitalAssetQueryHandler>,
          IRequestHandler<GetPurchaseDigitalAssetQuery, AppResult<DigitalAssetFileDescriptor>>
    {
        private readonly IBookPurchaseRepository _purchaseRepository;
        private readonly IFileAccessService _fileAccessService;

        public GetPurchaseDigitalAssetQueryHandler(
            IBookPurchaseRepository purchaseRepository,
            IFileAccessService fileAccessService,
            ILogger<GetPurchaseDigitalAssetQueryHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _purchaseRepository = purchaseRepository;
            _fileAccessService = fileAccessService;
        }

        public Task<AppResult<DigitalAssetFileDescriptor>> Handle(
            GetPurchaseDigitalAssetQuery request,
            CancellationToken cancellationToken) =>
            ExecuteAsync(request, () => ProcessAsync(request, cancellationToken),
                "Digital asset resolved successfully.");

        private async Task<DigitalAssetFileDescriptor> ProcessAsync(
            GetPurchaseDigitalAssetQuery request,
            CancellationToken cancellationToken)
        {
            var info = await _purchaseRepository.GetDigitalAssetInfoAsync(request.PurchaseId, cancellationToken);

            // A missing purchase and someone else's purchase both surface as 404: this
            // keeps the endpoint from letting a caller enumerate valid purchase IDs.
            if (info is null || info.UserId != request.RequestingUserId)
            {
                throw new NotFoundException("Purchase not found.");
            }

            if (string.IsNullOrWhiteSpace(info.PurchasedDigitalAssetUrl))
            {
                throw new DomainException("This purchase does not include a digital download.");
            }

            // The descriptor's ContentLength/ETag/LastModifiedUtc come from a filesystem
            // stat performed inside TryPrepareDownload — the controller streams the file
            // and answers conditional/range requests from that metadata without doing
            // any I/O of its own.
            if (!_fileAccessService.TryPrepareDownload(
                    info.PurchasedDigitalAssetUrl, info.BookTitle, out var descriptor))
            {
                throw new NotFoundException("The purchased file is no longer available. Please contact support.");
            }

            return descriptor;
        }
    }
}
