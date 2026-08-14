using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Payouts.Queries.GetLibraryWallet
{
    public class GetLibraryWalletQueryHandler
        : BaseApplicationService<GetLibraryWalletQueryHandler>,
          IRequestHandler<GetLibraryWalletQuery, AppResult<LibraryWalletResponse>>
    {
        private readonly ILibraryRepository _libraryRepository;

        public GetLibraryWalletQueryHandler(
            ILibraryRepository libraryRepository,
            ILogger<GetLibraryWalletQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
        }

        public async Task<AppResult<LibraryWalletResponse>> Handle(
            GetLibraryWalletQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var library = await _libraryRepository.GetApprovedByUserIdAsync(
                    request.UserId,
                    cancellationToken);

                if (library is null)
                {
                    throw new NotFoundException("Library not found");
                }

                return new LibraryWalletResponse(
                    library.StripeConnectAccountId,
                    library.StripeConnectAccountId is not null);
            }, "Stripe wallet retrieved successfully");
        }
    }
}
