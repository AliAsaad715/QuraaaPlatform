using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Categories.Common;
using Quraaa.Application.Features.Categories.Interfaces;
using Quraaa.Application.Shared.Persistence;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Category;

namespace Quraaa.Application.Features.Categories.Commands.CreateCategory
{
    public class CreateCategoryCommandHandler : BaseApplicationService<CreateCategoryCommandHandler>, IRequestHandler<CreateCategoryCommand, AppResult>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateCategoryCommandHandler(
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork,
            ILogger<CreateCategoryCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<AppResult> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var category = new CategoryAggregate(
                    Guid.NewGuid(),
                    request.Code,
                    request.NameAr,
                    request.NameEn,
                    request.ParentCategoryId
                );
                await _categoryRepository.AddAsync(category, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }, "Category created successfully");
        }
    }
}