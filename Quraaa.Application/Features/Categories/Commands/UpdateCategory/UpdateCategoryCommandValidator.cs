using FluentValidation;
using Quraaa.Application.Features.Categories.Interfaces;

namespace Quraaa.Application.Features.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
    {
        public UpdateCategoryCommandValidator(ICategoryRepository categoryRepository)
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Category id is required.");

            RuleFor(x => x.NameAr)
                .NotEmpty().WithMessage("Arabic name is required.")
                .MaximumLength(100).WithMessage("Arabic name must not exceed 100 characters.");

            RuleFor(x => x.NameEn)
                .NotEmpty().WithMessage("English name is required.")
                .MaximumLength(100).WithMessage("English name must not exceed 100 characters.");

            RuleFor(x => x)
                .MustAsync(async (command, cancellationToken) =>
                    !await categoryRepository.ExistsByNameExcludingIdAsync(
                        command.NameAr, command.NameEn, command.Id, cancellationToken))
                .WithMessage("Another category already uses this Arabic or English name.")
                .WithName("Name")
                .When(x => !string.IsNullOrWhiteSpace(x.NameAr) && !string.IsNullOrWhiteSpace(x.NameEn));
        }
    }
}
