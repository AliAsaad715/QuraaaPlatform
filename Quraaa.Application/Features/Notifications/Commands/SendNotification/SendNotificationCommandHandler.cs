using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Notifications.Common;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Notifications.Commands.SendNotification
{
    public class SendNotificationCommandHandler : BaseApplicationService<SendNotificationCommandHandler>, IRequestHandler<SendNotificationCommand, AppResult<NotificationSendResponse>>
    {
        private readonly IFirebaseNotificationService _firebaseNotificationService;
        private readonly IUserRepository _userRepository;

        public SendNotificationCommandHandler(
            IFirebaseNotificationService firebaseNotificationService,
            IUserRepository userRepository,
            ILogger<SendNotificationCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _firebaseNotificationService = firebaseNotificationService;
            _userRepository = userRepository;
        }

        public async Task<AppResult<NotificationSendResponse>> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync<SendNotificationCommand, NotificationSendResponse>(request, async () =>
            {
                var user = await _userRepository.GetUserByIdAsync(request.UserId);
                if (user is null)
                {
                    throw new NotFoundException("User was not found.");
                }

                var messageId = await _firebaseNotificationService.SendToDeviceAsync(
                    request.DeviceToken,
                    request.Title,
                    request.Body,
                    request.Data,
                    cancellationToken);

                return new NotificationSendResponse(messageId);
            }, "Notification sent successfully");
        }
    }
}
