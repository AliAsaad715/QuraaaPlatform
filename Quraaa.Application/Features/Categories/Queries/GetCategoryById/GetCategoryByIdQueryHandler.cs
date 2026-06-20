using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Categories.Common;
using Quraaa.Application.Features.Categories.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Categories.Queries.GetCategoryById
{
    public class GetCategoryByIdQueryHandler : BaseApplicationService<GetCategoryByIdQueryHandler>, IRequestHandler<GetCategoryByIdQuery, AppResult<CategoryResponse>>
    {
        private readonly ICategoryRepository _categoryRepository;

        public GetCategoryByIdQueryHandler(
            ICategoryRepository categoryRepository,
            ILogger<GetCategoryByIdQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<AppResult<CategoryResponse>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetCategoryByIdQuery, CategoryResponse>(request, async () =>
            {
                var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
                if (category == null)
                {
                    throw new NotFoundException($"Category withID {request.CategoryId} was not found.");
                }

                return new CategoryResponse(
                    category.Id,
                    category.NameAr,
                    category.NameEn
                );
            }, "Category retrieved successfully");
        }
    }
}