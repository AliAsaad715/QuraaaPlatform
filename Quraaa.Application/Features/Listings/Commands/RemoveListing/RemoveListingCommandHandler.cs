using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Listings.Commands.RemoveListing
{
    public class RemoveListingCommandHandler
        : BaseApplicationService<RemoveListingCommandHandler>,
          IRequestHandler<RemoveListingCommand, AppResult>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IListingRepository _listingRepository;
        private readonly IOrderRepository _orderRepository;

        public RemoveListingCommandHandler(
            ILibraryRepository libraryRepository,
            IListingRepository listingRepository,
            IOrderRepository orderRepository,
            ILogger<RemoveListingCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _listingRepository = listingRepository;
            _orderRepository = orderRepository;
        }

        public async Task<AppResult> Handle(
            RemoveListingCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var listing = await _listingRepository.GetByIdForInventoryAsync(
                    request.ListingId,
                    cancellationToken);

                if (listing is null)
                {
                    throw new NotFoundException("Listing not found");
                }

                var library = await _libraryRepository.GetApprovedByUserIdAsync(
                    request.RequestingUserId,
                    cancellationToken);

                if (library is null || listing.LibraryId != library.Id)
                {
                    throw new UnauthorizedAccessException("Unauthorized access to this listing");
                }

                if (await _orderRepository.HasActiveStockReservationAsync(
                    listing.Id,
                    cancellationToken))
                {
                    throw new ConflictException(
                        "Listing cannot be removed while pending orders reserve this listing.");
                }

                listing.Remove(request.RequestingUserId);
                await _listingRepository.SaveChangesAsync(cancellationToken);

            }, "Listing removed successfully.");
        }
    }
}