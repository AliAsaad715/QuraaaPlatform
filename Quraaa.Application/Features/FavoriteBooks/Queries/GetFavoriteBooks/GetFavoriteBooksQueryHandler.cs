using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.FavoriteBooks.Common;
using Quraaa.Application.Features.FavoriteBooks.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.FavoriteBooks.Queries.GetFavoriteBooks
{
    public class GetFavoriteBooksQueryHandler
        : BaseApplicationService<GetFavoriteBooksQueryHandler>,
          IRequestHandler<GetFavoriteBooksQuery, AppResult<PagedResult<FavoriteBookResponse>>>
    {
        private readonly IFavoriteBookRepository _favoriteBookRepository;
        private readonly IUserRepository _userRepository;

        public GetFavoriteBooksQueryHandler(
            IFavoriteBookRepository favoriteBookRepository,
            IUserRepository userRepository,
            ILogger<GetFavoriteBooksQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _favoriteBookRepository = favoriteBookRepository;
            _userRepository = userRepository;
        }

        public async Task<AppResult<PagedResult<FavoriteBookResponse>>> Handle(
            GetFavoriteBooksQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetFavoriteBooksQuery, PagedResult<FavoriteBookResponse>>(request, async () =>
            {
                var user = await _userRepository.GetUserByIdAsync(request.UserId);
                if (user is null)
                {
                    throw new NotFoundException("User was not found.");
                }

                var (favoriteBooks, totalCount) = await _favoriteBookRepository.GetPagedAsync(
                    request.UserId,
                    request.PageNumber,
                    request.PageSize,
                    request.SearchTerm,
                    cancellationToken);

                return new PagedResult<FavoriteBookResponse>(
                    favoriteBooks,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }, "Favorite books retrieved successfully");
        }
    }
}
