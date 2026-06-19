using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Categories.Common;
using Quraaa.Application.Features.Categories.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Category;

namespace Quraaa.Application.Features.Categories.Commands.CreateCategory
{
    public class CreateCategoryCommandHandler : BaseApplicationService<CreateCategoryCommandHandler>, IRequestHandler<CreateCategoryCommand, AppResult<CategoryResponse>>
    {
        private readonly ICategoryRepository _categoryRepository;

        public CreateCategoryCommandHandler(
            ICategoryRepository categoryRepository,
            ILogger<CreateCategoryCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<AppResult<CategoryResponse>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync<CreateCategoryCommand, CategoryResponse>(request, async () =>
            {
                var category = new CategoryAggregate(
                    Guid.NewGuid(),
                    request.Code,
                    request.NameAr,
                    request.NameEn,
                    request.ParentCategoryId
                );
                await _categoryRepository.AddAsync(category, cancellationToken);
                return new CategoryResponse(
                    category.Id,
                    category.Code,
                    category.NameAr,
                    category.NameEn,
                    category.ParentCategoryId,
                    category.IsActive
                );
            }, "Category created successfully");
        }
    }
}