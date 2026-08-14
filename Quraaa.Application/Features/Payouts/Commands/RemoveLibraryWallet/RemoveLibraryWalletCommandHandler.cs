using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Payouts.Commands.RemoveLibraryWallet
{
    public class RemoveLibraryWalletCommandHandler
        : BaseApplicationService<RemoveLibraryWalletCommandHandler>,
          IRequestHandler<RemoveLibraryWalletCommand, AppResult>
    {
        private readonly ILibraryRepository _libraryRepository;

        public RemoveLibraryWalletCommandHandler(
            ILibraryRepository libraryRepository,
            ILogger<RemoveLibraryWalletCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
        }

        public async Task<AppResult> Handle(
            RemoveLibraryWalletCommand request,
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

                library.RemoveStripeWallet(request.UserId);
                await _libraryRepository.SaveChangesAsync();
            }, "Stripe wallet removed successfully");
        }
    }
}
