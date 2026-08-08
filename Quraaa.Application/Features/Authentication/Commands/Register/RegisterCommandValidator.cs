using FluentValidation;
using PhoneNumbers;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Features.Categories.Interfaces;

namespace Quraaa.Application.Features.Authentication.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        private readonly ICategoryRepository _categoryRepository;

        public RegisterCommandValidator(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;

            // 1. Validate first name and last name
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");

            // 2. Validate phone number — only Syrian numbers (+963)
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Must(BeASyrianPhoneNumber)
                .WithMessage("Invalid Syrian phone number. It must start with '+963' and be a valid Syrian number.");

            // 3. Validate password strength to meet Identity requirements and avoid early exceptions
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(AuthenticationPasswordPolicy.MinimumLength)
                    .WithMessage($"Password must be at least {AuthenticationPasswordPolicy.MinimumLength} characters long.")
                .MaximumLength(AuthenticationPasswordPolicy.MaximumLength)
                    .WithMessage($"Password must not exceed {AuthenticationPasswordPolicy.MaximumLength} characters.")
                .Must(AuthenticationPasswordPolicy.MeetsComplexityRequirements)
                    .WithMessage("Password must contain an uppercase letter, a lowercase letter, a digit, and a non-alphanumeric character.");

            // 4. Validate date of birth (prevent future dates, ensure reasonable age)
            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage("Date of birth is required.")
                .Must(BeAValidAge).WithMessage("Date of birth is invalid or the user's age is too young.");

            // 5. Validate Gender
            RuleFor(x => x.Gender)
                .IsInEnum().WithMessage("The provided gender value is invalid.");

            // 6. Validate that at least one interest is selected
            RuleFor(x => x.Interests)
                .NotEmpty().WithMessage("You must select at least one interest.");

            // 7. Validate interests exist in the system
            RuleFor(x => x.Interests)
                .MustAsync(BeValidCategoryIds)
                .WithMessage("One or more interest IDs do not exist in the system.");

        }

        private bool BeASyrianPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            var trimmed = phoneNumber.Trim();

            // Strict prefix check for Syria
            if (!trimmed.StartsWith("+963"))
                return false;

            var phoneUtil = PhoneNumberUtil.GetInstance();
            try
            {
                // Parse using 'SY' region — ensures library applies Syrian rules.
                var number = phoneUtil.Parse(trimmed, "SY");

                // Ensure the parsed number is valid and belongs to Syria
                return phoneUtil.IsValidNumberForRegion(number, "SY");
            }
            catch (NumberParseException)
            {
                return false;
            }
        }

        // Helper method to validate age (e.g., prevent future dates, ensure minimum age of 5 years)
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
