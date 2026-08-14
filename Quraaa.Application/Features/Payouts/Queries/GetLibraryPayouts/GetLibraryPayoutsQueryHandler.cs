using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Features.Payouts.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Payouts.Queries.GetLibraryPayouts
{
    public class GetLibraryPayoutsQueryHandler
        : BaseApplicationService<GetLibraryPayoutsQueryHandler>,
          IRequestHandler<GetLibraryPayoutsQuery, AppResult<PagedResult<SellerPayoutResponse>>>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly ISellerPayoutRepository _sellerPayoutRepository;

        public GetLibraryPayoutsQueryHandler(
            ILibraryRepository libraryRepository,
            ISellerPayoutRepository sellerPayoutRepository,
            ILogger<GetLibraryPayoutsQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _sellerPayoutRepository = sellerPayoutRepository;
        }

        public async Task<AppResult<PagedResult<SellerPayoutResponse>>> Handle(
            GetLibraryPayoutsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetLibraryPayoutsQuery, PagedResult<SellerPayoutResponse>>(request, async () =>
            {
                var library = await _libraryRepository.GetApprovedByUserIdAsync(
                    request.RequestingUserId,
                    cancellationToken);

                if (library is null)
                {
                    throw new NotFoundException("Library not found");
                }

                var (items, totalCount) = await _sellerPayoutRepository
                    .GetPagedForLibraryAsync(
                        library.Id,
                        request.PageNumber,
                        request.PageSize,
                        cancellationToken);

                return new PagedResult<SellerPayoutResponse>(
                    items,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }, "Payouts retrieved successfully");
        }
    }
}
