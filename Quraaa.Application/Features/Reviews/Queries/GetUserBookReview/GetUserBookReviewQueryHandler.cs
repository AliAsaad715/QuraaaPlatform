using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Reviews.Common;
using Quraaa.Application.Features.Reviews.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Reviews.Queries.GetUserBookReview
{
    public class GetUserBookReviewQueryHandler
        : BaseApplicationService<GetUserBookReviewQueryHandler>,
          IRequestHandler<GetUserBookReviewQuery, AppResult<BookReviewResponse>>
    {
        private readonly IBookReviewRepository _bookReviewRepository;
        private readonly IUserRepository _userRepository;
        private readonly IImageUrlFormatter _imageUrlFormatter;

        public GetUserBookReviewQueryHandler(
            IBookReviewRepository bookReviewRepository,
            IUserRepository userRepository,
            IImageUrlFormatter imageUrlFormatter,
            ILogger<GetUserBookReviewQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _bookReviewRepository = bookReviewRepository;
            _userRepository = userRepository;
            _imageUrlFormatter = imageUrlFormatter;
        }

        public async Task<AppResult<BookReviewResponse>> Handle(
            GetUserBookReviewQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetUserBookReviewQuery, BookReviewResponse>(request, async () =>
            {
                var review = await _bookReviewRepository.GetByUserAndBookAsync(request.UserId, request.BookId, cancellationToken);
                if (review is null)
                {
                    throw new NotFoundException("No existing review found for this book.");
                }

                var user = await _userRepository.GetUserByIdAsync(request.UserId);
                if (user is null)
                {
                    throw new NotFoundException("User was not found.");
                }

                return new BookReviewResponse(
                    review.Id,
                    review.UserId,
                    $"{user.FirstName} {user.LastName}",
                    _imageUrlFormatter.Format(user.ProfileImageUrl),
                    review.Score,
                    review.Content,
                    review.CreationTime);
            }, "Review retrieved successfully");
        }
    }
}
