using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.FavoriteBooks.Common;
using Quraaa.Application.Features.FavoriteBooks.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Favorites;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.FavoriteBooks.Commands.AddFavoriteBook
{
    public class AddFavoriteBookCommandHandler
        : BaseApplicationService<AddFavoriteBookCommandHandler>,
          IRequestHandler<AddFavoriteBookCommand, AppResult<FavoriteBookResponse>>
    {
        private readonly IFavoriteBookRepository _favoriteBookRepository;
        private readonly IUserRepository _userRepository;

        public AddFavoriteBookCommandHandler(
            IFavoriteBookRepository favoriteBookRepository,
            IUserRepository userRepository,
            ILogger<AddFavoriteBookCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _favoriteBookRepository = favoriteBookRepository;
            _userRepository = userRepository;
        }

        public async Task<AppResult<FavoriteBookResponse>> Handle(
            AddFavoriteBookCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<AddFavoriteBookCommand, FavoriteBookResponse>(request, async () =>
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

                var existingFavorite = await _favoriteBookRepository.GetFavoriteAsync(
                    request.UserId,
                    request.BookId,
                    cancellationToken);

                if (existingFavorite is not null)
                {
                    return existingFavorite;
                }

                var favoriteBook = FavoriteBookAggregate.Create(request.UserId, request.BookId);

                await _favoriteBookRepository.AddAsync(favoriteBook, cancellationToken);

                try
                {
                    await _favoriteBookRepository.SaveChangesAsync(cancellationToken);
                }
                catch (ApplicationBusinessException ex) when (
                    string.Equals(ex.Message, FavoriteBookErrorCodes.DuplicateFavoriteBook, StringComparison.Ordinal))
                {
                    var racedFavorite = await _favoriteBookRepository.GetFavoriteAsync(
                        request.UserId,
                        request.BookId,
                        cancellationToken);

                    if (racedFavorite is not null)
                    {
                        return racedFavorite;
                    }

                    throw;
                }

                return await _favoriteBookRepository.GetFavoriteAsync(
                    request.UserId,
                    request.BookId,
                    cancellationToken)
                    ?? throw new NotFoundException("Favorite book was not found after creation.");
            }, "Favorite book added successfully");
        }
    }
}
