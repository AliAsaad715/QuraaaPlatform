using FluentValidation;
using Quraaa.Application.Features.Categories.Interfaces;

namespace Quraaa.Application.Features.Profiles.Commands.UpdateProfile
{
    public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
    {
        private readonly ICategoryRepository _categoryRepository;

        public UpdateProfileCommandValidator(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");

            RuleFor(x => x.Gender)
                .IsInEnum().WithMessage("The provided gender value is invalid.");

            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage("Date of birth is required.")
                .Must(BeAValidAge).WithMessage("Date of birth is invalid or the user's age is too young.");

            RuleFor(x => x.ProfileImageUrl)
                .MaximumLength(500).WithMessage("Profile image URL must not exceed 500 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.ProfileImageUrl));

            RuleFor(x => x.Interests)
                .NotEmpty().WithMessage("You must select at least one interest.");

            // Validate interests exist in the system
            RuleFor(x => x.Interests)
                .MustAsync(BeValidCategoryIds)
                .WithMessage("One or more interest IDs do not exist in the system.");
        }

        private bool BeAValidAge(DateOnly dateOfBirth)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var minDate = today.AddYears(-100);
            var maxDate = today.AddYears(-5);

            return dateOfBirth > minDate && dateOfBirth <= maxDate;
        }

        private async Task<bool> BeValidCategoryIds(List<Guid> interests, CancellationToken cancellationToken)
        {
            if (interests == null || !interests.Any())
                return true;

            var existingCategories = await _categoryRepository.GetByIdsAsync(interests, cancellationToken);
            return existingCategories.Count == interests.Count;
        }
    }
}