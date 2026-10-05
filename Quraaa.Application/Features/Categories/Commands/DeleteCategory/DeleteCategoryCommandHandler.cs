using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Categories.Interfaces;
using Quraaa.Application.Shared.Persistence;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Categories.Commands.DeleteCategory
{
    public class DeleteCategoryCommandHandler
        : BaseApplicationService<DeleteCategoryCommandHandler>,
          IRequestHandler<DeleteCategoryCommand, AppResult>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteCategoryCommandHandler(
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork,
            ILogger<DeleteCategoryCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<AppResult> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken)
                    ?? throw new NotFoundException($"Category with ID {request.Id} was not found.");

                var hasLinkedBooks = await _categoryRepository.HasLinkedBooksAsync(request.Id, cancellationToken);
                if (hasLinkedBooks)
                {
                    throw new ConflictException(
                        "This category cannot be deleted because one or more books still reference it.");
                }

                await _categoryRepository.RemoveAsync(category, cancellationToken);

                // HasLinkedBooksAsync can race a concurrent book insert; the
                // foreign key then fails the save with the same ConflictException.
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }, "Category deleted successfully");
        }
    }
}
