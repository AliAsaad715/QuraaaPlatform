using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Categories.Common;
using Quraaa.Application.Features.Categories.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesQueryHandler : BaseApplicationService<GetAllCategoriesQueryHandler>, IRequestHandler<GetAllCategoriesQuery, AppResult<List<CategoryResponse>>>
    {
        private readonly ICategoryRepository _categoryRepository;

        public GetAllCategoriesQueryHandler(
            ICategoryRepository categoryRepository,
            ILogger<GetAllCategoriesQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<AppResult<List<CategoryResponse>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetAllCategoriesQuery, List<CategoryResponse>>(request, async () =>
            {
                var categories = await _categoryRepository.GetAllAsync(cancellationToken);
                var categoryResponses = categories
                    .Select(c => new CategoryResponse(
                        c.Id,
                        c.Code,
                        c.NameAr,
                        c.NameEn,
                        c.ParentCategoryId,
                        c.IsActive))
                    .ToList();

                return categoryResponses;
            }, "Categories retrieved successfully");
        }
    }
}