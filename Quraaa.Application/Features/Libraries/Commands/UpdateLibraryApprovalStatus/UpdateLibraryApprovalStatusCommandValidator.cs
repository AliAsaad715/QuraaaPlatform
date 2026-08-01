using FluentValidation;
using Quraaa.Domain.Library.Enums;

namespace Quraaa.Application.Features.Libraries.Commands.UpdateLibraryApprovalStatus
{
    public sealed class UpdateLibraryApprovalStatusCommandValidator : AbstractValidator<UpdateLibraryApprovalStatusCommand>
    {
        public UpdateLibraryApprovalStatusCommandValidator()
        {
            RuleFor(x => x.LibraryId)
                .NotEmpty()
                .WithMessage("Library ID is required.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid approval status.")
                .Must(status => status == LibraryApprovalStatus.Approved || status == LibraryApprovalStatus.Rejected)
                .WithMessage("Status must be either Approved or Rejected.");
        }
    }
}