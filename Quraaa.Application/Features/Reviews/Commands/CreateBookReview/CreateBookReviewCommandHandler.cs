using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Application.Features.Reviews.Common;
using Quraaa.Application.Features.Reviews.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Reviews;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Reviews.Commands.CreateBookReview
{
    public class CreateBookReviewCommandHandler
        : BaseApplicationService<CreateBookReviewCommandHandler>,
          IRequestHandler<CreateBookReviewCommand, AppResult<BookReviewResponse>>
    {
        private const string AlreadyReviewedMessage =
            "You have already reviewed this book. Use the update endpoint to modify your review.";

        private readonly IBookReviewRepository _bookReviewRepository;
        private readonly IBookPurchaseRepository _bookPurchaseRepository;
        private readonly IUserRepository _userRepository;
        private readonly IImageUrlFormatter _imageUrlFormatter;

        public CreateBookReviewCommandHandler(
            IBookReviewRepository bookReviewRepository,
            IBookPurchaseRepository bookPurchaseRepository,
            IUserRepository userRepository,
            IImageUrlFormatter imageUrlFormatter,
            ILogger<CreateBookReviewCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _bookReviewRepository = bookReviewRepository;
            _bookPurchaseRepository = bookPurchaseRepository;
            _userRepository = userRepository;
            _imageUrlFormatter = imageUrlFormatter;
        }

        public async Task<AppResult<BookReviewResponse>> Handle(
            CreateBookReviewCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<CreateBookReviewCommand, BookReviewResponse>(request, async () =>
            {
                var user = await _userRepository.GetUserByIdAsync(request.UserId);
                if (user is null)
                {
                    throw new NotFoundException("User was not found.");
                }

                if (!await _bookReviewRepository.BookExistsAsync(request.BookId, cancellationToken))
                {
                    throw new NotFoundException("Book was not found.");
                }

                if (!await _bookPurchaseRepository.HasUserPurchasedBookAsync(request.UserId, request.BookId, cancellationToken))
                {
                    throw new ApplicationBusinessException(
                        "You must purchase this book before leaving a review.",
                        nameof(CreateBookReviewCommand.BookId));
                }

                var existingReview = await _bookReviewRepository.GetByUserAndBookAsync(request.UserId, request.BookId, cancellationToken);
                if (existingReview is not null)
                {
                    throw new ApplicationBusinessException(AlreadyReviewedMessage, nameof(CreateBookReviewCommand.BookId));
                }

                var review = BookReviewAggregate.Create(request.UserId, request.BookId, request.Score, request.Content);
                await _bookReviewRepository.AddAsync(review, cancellationToken);

                try
                {
                    await _bookReviewRepository.SaveChangesAsync(cancellationToken);
                }
                catch (ApplicationBusinessException ex) when (
                    string.Equals(ex.Message, ReviewErrorCodes.DuplicateReview, StringComparison.Ordinal))
                {
                    throw new ApplicationBusinessException(AlreadyReviewedMessage, nameof(CreateBookReviewCommand.BookId));
                }

                return new BookReviewResponse(
                    review.Id,
                    review.UserId,
                    $"{user.FirstName} {user.LastName}",
                    _imageUrlFormatter.Format(user.ProfileImageUrl),
                    review.Score,
                    review.Content,
                    review.CreationTime);
            }, "Review submitted successfully");
        }
    }
}
