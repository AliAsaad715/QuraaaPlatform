using FluentValidation;

namespace Quraaa.Application.Features.Notifications.Commands.SendNotification
{
    public class SendNotificationCommandValidator : AbstractValidator<SendNotificationCommand>
    {
        public SendNotificationCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");

            RuleFor(x => x.DeviceToken)
                .NotEmpty().WithMessage("Device token is required.")
                .MaximumLength(4096).WithMessage("Device token is too long.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Notification title is required.")
                .MaximumLength(100).WithMessage("Notification title must not exceed 100 characters.");

            RuleFor(x => x.Body)
                .NotEmpty().WithMessage("Notification body is required.")
                .MaximumLength(500).WithMessage("Notification body must not exceed 500 characters.");

            RuleFor(x => x.Data)
                .Must(data => data is null || data.Count <= 20)
                .WithMessage("Notification data must not contain more than 20 entries.");

            RuleForEach(x => x.Data)
                .ChildRules(entry =>
                {
                    entry.RuleFor(x => x.Key)
                        .NotEmpty().WithMessage("Notification data keys must not be empty.")
                        .MaximumLength(128).WithMessage("Notification data keys must not exceed 128 characters.")
                        .Must(BeAllowedFirebaseDataKey)
                        .WithMessage("Notification data keys must not be reserved Firebase keys.");

                    entry.RuleFor(x => x.Value)
                        .NotNull().WithMessage("Notification data values must not be null.")
                        .MaximumLength(1024).WithMessage("Notification data values must not exceed 1024 characters.");
                });
        }

        private static bool BeAllowedFirebaseDataKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return !string.Equals(key, "from", StringComparison.OrdinalIgnoreCase)
                && !key.StartsWith("google", StringComparison.OrdinalIgnoreCase);
        }
    }
}
