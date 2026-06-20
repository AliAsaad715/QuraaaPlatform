using FluentValidation;
using PhoneNumbers;
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

            // 2. Validate phone number (adjust the regex for targeted country formats)
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Must(BeAValidInternationalPhoneNumber)
                .WithMessage("Invalid international phone number format. It must start with '+' and include a valid country code.");

            // 3. Validate password strength to meet Identity requirements and avoid early exceptions
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");

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

        private bool BeAValidInternationalPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return false;

            // Strict check: must start with '+'
            if (!phoneNumber.Trim().StartsWith("+")) return false;

            var phoneUtil = PhoneNumberUtil.GetInstance();
            try
            {
                // Parse without a default region to force the library to read the country code from the input
                var number = phoneUtil.Parse(phoneNumber, null);

                // Validate number according to the extracted country's rules (length, possible range, etc.)
                return phoneUtil.IsValidNumber(number);
            }
            catch (NumberParseException)
            {
                return false; // Input is not a valid phone number
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
