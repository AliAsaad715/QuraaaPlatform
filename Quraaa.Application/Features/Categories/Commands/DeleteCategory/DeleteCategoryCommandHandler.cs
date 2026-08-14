using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Categories.Interfaces;
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

        public DeleteCategoryCommandHandler(
            ICategoryRepository categoryRepository,
            ILogger<DeleteCategoryCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _categoryRepository = categoryRepository;
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
            }, "Category deleted successfully");
        }
    }
}
