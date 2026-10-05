using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Reviews.Interfaces;
using Quraaa.Application.Shared.Persistence;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Reviews.Commands.DeleteBookReview
{
    public class DeleteBookReviewCommandHandler
        : BaseApplicationService<DeleteBookReviewCommandHandler>,
          IRequestHandler<DeleteBookReviewCommand, AppResult>
    {
        private readonly IBookReviewRepository _bookReviewRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteBookReviewCommandHandler(
            IBookReviewRepository bookReviewRepository,
            IUnitOfWork unitOfWork,
            ILogger<DeleteBookReviewCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _bookReviewRepository = bookReviewRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<AppResult> Handle(
            DeleteBookReviewCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var review = await _bookReviewRepository.GetByUserAndBookAsync(request.UserId, request.BookId, cancellationToken);
                if (review is null)
                {
                    throw new NotFoundException("No existing review found for this book.");
                }

                review.Delete(request.UserId);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }, "Review deleted successfully");
        }
    }
}
