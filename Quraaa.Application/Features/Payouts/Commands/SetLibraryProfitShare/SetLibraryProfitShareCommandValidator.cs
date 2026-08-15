using FluentValidation;
using Quraaa.Domain.Library;

namespace Quraaa.Application.Features.Payouts.Commands.SetLibraryProfitShare
{
    public sealed class SetLibraryProfitShareCommandValidator
        : AbstractValidator<SetLibraryProfitShareCommand>
    {
        public SetLibraryProfitShareCommandValidator()
        {
            RuleFor(x => x.LibraryId)
                .NotEmpty()
                .WithMessage("Library id is required.");

            RuleFor(x => x.AdminId)
                .NotEmpty()
                .WithMessage("Admin id is required.");

            RuleFor(x => x.ProfitSharePercent)
                .InclusiveBetween(0m, 100m)
                .WithMessage("The profit share percentage must be between 0 and 100.")
                .Must(percent => decimal.Round(percent, LibraryAggregate.ProfitSharePercentScale) == percent)
                .WithMessage(
                    $"The profit share percentage may have at most {LibraryAggregate.ProfitSharePercentScale} decimal places.");
        }
    }
}
