using FluentValidation;
using Quraaa.Application.Features.Authentication.Common;

namespace Quraaa.Application.Features.Libraries.Common
{
    /// <summary>
    /// The library dashboard password rules, shared by registration, change,
    /// and reset so all three enforce exactly the same policy.
    /// </summary>
    public static class LibraryPasswordRules
    {
        public const string MustDifferFromAccountPasswordMessage =
            "The library password must be different from your account password.";

        public static IRuleBuilderOptions<T, string> ApplyPasswordRules<T>(
            this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("A library password is required.")
                .MinimumLength(AuthenticationPasswordPolicy.MinimumLength)
                    .WithMessage($"The library password must be at least {AuthenticationPasswordPolicy.MinimumLength} characters long.")
                .MaximumLength(AuthenticationPasswordPolicy.MaximumLength)
                    .WithMessage($"The library password must not exceed {AuthenticationPasswordPolicy.MaximumLength} characters.")
                .Must(AuthenticationPasswordPolicy.MeetsComplexityRequirements)
                    .WithMessage("The library password must contain an uppercase letter, a lowercase letter, a digit, and a non-alphanumeric character.");
        }
    }
}
