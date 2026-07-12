using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Books.Queries.GetRecommendedBooks
{
    public class GetRecommendedBooksQueryHandler
        : BaseApplicationService<GetRecommendedBooksQueryHandler>,
          IRequestHandler<GetRecommendedBooksQuery, AppResult<PagedResult<PopularBookResponse>>>
    {
        private readonly IBookPopularityRepository _bookPopularityRepository;
        private readonly IUserRepository _userRepository;

        public GetRecommendedBooksQueryHandler(
            IBookPopularityRepository bookPopularityRepository,
            IUserRepository userRepository,
            ILogger<GetRecommendedBooksQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _bookPopularityRepository = bookPopularityRepository;
            _userRepository = userRepository;
        }

        public async Task<AppResult<PagedResult<PopularBookResponse>>> Handle(
            GetRecommendedBooksQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetRecommendedBooksQuery, PagedResult<PopularBookResponse>>(request, async () =>
            {
                var user = await _userRepository.GetUserByIdAsync(request.UserId);
                if (user is null)
                {
                    throw new NotFoundException("User was not found.");
                }

                var interestedCategoryIds = user.InterestedCategoryIds;
                if (interestedCategoryIds.Count == 0)
                {
                    return new PagedResult<PopularBookResponse>(
                        Array.Empty<PopularBookResponse>(),
                        request.PageNumber,
                        request.PageSize,
                        0);
                }

                var (books, totalCount) = await _bookPopularityRepository.GetRecommendedAsync(
                    interestedCategoryIds,
                    request.Language,
                    request.PageNumber,
                    request.PageSize,
                    request.SearchTerm,
                    cancellationToken);

                return new PagedResult<PopularBookResponse>(
                    books,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }, "Recommended books retrieved successfully");
        }
    }
}
