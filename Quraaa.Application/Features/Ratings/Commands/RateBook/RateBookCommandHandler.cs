using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Application.Features.Ratings.Common;
using Quraaa.Application.Features.Ratings.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Ratings;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Ratings.Commands.RateBook
{
    public class RateBookCommandHandler
        : BaseApplicationService<RateBookCommandHandler>,
          IRequestHandler<RateBookCommand, AppResult<BookRatingResponse>>
    {
        private readonly IBookRatingRepository _bookRatingRepository;
        private readonly IBookPurchaseRepository _bookPurchaseRepository;
        private readonly IUserRepository _userRepository;

        public RateBookCommandHandler(
            IBookRatingRepository bookRatingRepository,
            IBookPurchaseRepository bookPurchaseRepository,
            IUserRepository userRepository,
            ILogger<RateBookCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _bookRatingRepository = bookRatingRepository;
            _bookPurchaseRepository = bookPurchaseRepository;
            _userRepository = userRepository;
        }

        public async Task<AppResult<BookRatingResponse>> Handle(
            RateBookCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<RateBookCommand, BookRatingResponse>(request, async () =>
            {
                var user = await _userRepository.GetUserByIdAsync(request.UserId);
                if (user is null)
                {
                    throw new NotFoundException("User was not found.");
                }

                if (!await _bookRatingRepository.BookExistsAsync(request.BookId, cancellationToken))
                {
                    throw new NotFoundException("Book was not found.");
                }

                if (!await _bookPurchaseRepository.HasUserPurchasedBookAsync(request.UserId, request.BookId, cancellationToken))
                {
                    throw new ApplicationBusinessException(
                        "You must purchase this book before rating it.",
                        nameof(RateBookCommand.BookId));
                }

                var rating = await _bookRatingRepository.GetByUserAndBookAsync(request.UserId, request.BookId, cancellationToken);

                if (rating is not null)
                {
                    rating.UpdateRating(request.Score, request.UserId);
                    await _bookRatingRepository.SaveChangesAsync(cancellationToken);
                    return ToResponse(rating);
                }

                rating = BookRatingAggregate.Create(request.UserId, request.BookId, request.Score);
                await _bookRatingRepository.AddAsync(rating, cancellationToken);

                try
                {
                    await _bookRatingRepository.SaveChangesAsync(cancellationToken);
                }
                catch (ApplicationBusinessException ex) when (
                    string.Equals(ex.Message, RatingErrorCodes.DuplicateRating, StringComparison.Ordinal))
                {
                    var racedRating = await _bookRatingRepository.GetByUserAndBookAsync(request.UserId, request.BookId, cancellationToken);
                    if (racedRating is null)
                    {
                        throw;
                    }

                    racedRating.UpdateRating(request.Score, request.UserId);
                    await _bookRatingRepository.SaveChangesAsync(cancellationToken);
                    return ToResponse(racedRating);
                }

                return ToResponse(rating);
            }, "Book rated successfully");
        }

        private static BookRatingResponse ToResponse(BookRatingAggregate rating) =>
            new(rating.Id, rating.BookId, rating.UserId, rating.RatingValue, rating.CreationTime, rating.LastModificationTime);
    }
}
