using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Notifications;
using Quraaa.Application.Features.Notifications.Commands.RegisterPushDevice;
using Quraaa.Application.Features.Notifications.Commands.SendNotification;
using Quraaa.Application.Features.Notifications.Commands.SendTestNotification;
using Quraaa.Application.Features.Notifications.Commands.UnregisterPushDevice;
using Quraaa.Application.Features.Notifications.Common;

namespace Quraaa.API.Controllers
{
    [Authorize]
    public class NotificationsController : ApiClientController
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public NotificationsController(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        [HttpPut("devices")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RegisterDevice(
            [FromBody] RegisterPushDeviceRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new RegisterPushDeviceCommand(userId, request.DeviceToken),
                cancellationToken);

            return HandleResult(result);
        }

        [HttpDelete("devices")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnregisterDevice(
            [FromBody] UnregisterPushDeviceRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new UnregisterPushDeviceCommand(userId, request.DeviceToken),
                cancellationToken);

            return HandleResult(result);
        }

        [HttpPost("send")]
        [ProducesResponseType(typeof(NotificationSendResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Send([FromBody] SendNotificationRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var command = new SendNotificationCommand(
                userId,
                request.DeviceToken,
                request.Title,
                request.Body,
                request.Data);

            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("test")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(NotificationSendResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendTest([FromBody] SendTestNotificationRequest request)
        {
            if (!IsTestEndpointEnabled())
            {
                return NotFound(new
                {
                    type = "NotFound",
                    title = "Resource Not Found",
                    detail = "The notification test endpoint is disabled."
                });
            }

            var command = new SendTestNotificationCommand(
                request.DeviceToken,
                request.Title,
                request.Body,
                request.Data);

            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        private bool IsTestEndpointEnabled()
        {
            return _environment.IsDevelopment()
                || _configuration.GetValue<bool>("Notifications:AllowTestEndpoint");
        }
    }
}
