using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.FavoriteBooks.Interfaces;
using Quraaa.Application.Shared.Persistence;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.FavoriteBooks.Commands.RemoveFavoriteBook
{
    public class RemoveFavoriteBookCommandHandler
        : BaseApplicationService<RemoveFavoriteBookCommandHandler>,
          IRequestHandler<RemoveFavoriteBookCommand, AppResult>
    {
        private readonly IFavoriteBookRepository _favoriteBookRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveFavoriteBookCommandHandler(
            IFavoriteBookRepository favoriteBookRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ILogger<RemoveFavoriteBookCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _favoriteBookRepository = favoriteBookRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<AppResult> Handle(
            RemoveFavoriteBookCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var user = await _userRepository.GetUserByIdAsync(request.UserId);
                if (user is null)
                {
                    throw new NotFoundException("User was not found.");
                }

                if (!await _favoriteBookRepository.BookExistsAsync(request.BookId, cancellationToken))
                {
                    throw new NotFoundException("Book was not found.");
                }

                var removed = await _favoriteBookRepository.RemoveAsync(
                    request.UserId,
                    request.BookId,
                    cancellationToken);

                if (!removed)
                {
                    throw new NotFoundException("Favorite book was not found.");
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }, "Favorite book removed successfully");
        }
    }
}
