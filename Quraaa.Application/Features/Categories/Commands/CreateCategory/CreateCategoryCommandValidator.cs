using FluentValidation;
using Quraaa.Application.Features.Categories.Interfaces;

namespace Quraaa.Application.Features.Categories.Commands.CreateCategory
{
    public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryCommandValidator(ICategoryRepository categoryRepository)
        {
            RuleFor(x => x.Code)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Category code is required.")
                .MaximumLength(50).WithMessage("Category code must not exceed 50 characters.")
                .MustAsync(async (code, cancellationToken) =>
                    !await categoryRepository.ExistsByCodeAsync(code, cancellationToken))
                .WithMessage("Category code already exists.");

            RuleFor(x => x.NameAr)
                .NotEmpty().WithMessage("Arabic name is required.")
                .MaximumLength(100).WithMessage("Arabic name must not exceed 100 characters.");

            RuleFor(x => x.NameEn)
                .NotEmpty().WithMessage("English name is required.")
                .MaximumLength(100).WithMessage("English name must not exceed 100 characters.");

            RuleFor(x => x.ParentCategoryId)
                .MustAsync(async (parentCategoryId, cancellationToken) =>
                {
                    var parent = await categoryRepository.GetByIdAsync(parentCategoryId!.Value, cancellationToken);
                    return parent is not null;
                })
                .WithMessage("Parent category does not exist.")
                .When(x => x.ParentCategoryId.HasValue);
        }
    }
}