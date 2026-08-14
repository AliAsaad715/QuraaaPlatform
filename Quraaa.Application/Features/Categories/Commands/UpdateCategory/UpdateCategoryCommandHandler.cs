using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Categories.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryCommandHandler
        : BaseApplicationService<UpdateCategoryCommandHandler>,
          IRequestHandler<UpdateCategoryCommand, AppResult>
    {
        private readonly ICategoryRepository _categoryRepository;

        public UpdateCategoryCommandHandler(
            ICategoryRepository categoryRepository,
            ILogger<UpdateCategoryCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<AppResult> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken)
                    ?? throw new NotFoundException($"Category with ID {request.Id} was not found.");

                // Code and ParentCategoryId are set at creation time only — the domain
                // aggregate exposes no method to change them, so update is limited to names.
                category.UpdateDetails(request.NameAr, request.NameEn, request.ModifiedBy);

                await _categoryRepository.SaveChangesAsync(cancellationToken);
            }, "Category updated successfully");
        }
    }
}
