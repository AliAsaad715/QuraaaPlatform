using FluentValidation;

namespace Quraaa.Application.Features.Libraries.Commands.IssueLibraryRegistrationLink
{
    public sealed class IssueLibraryRegistrationLinkCommandValidator
        : AbstractValidator<IssueLibraryRegistrationLinkCommand>
    {
        public IssueLibraryRegistrationLinkCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.AuthenticationSessionId).NotEmpty();
        }
    }
}
