using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Notifications;
using Quraaa.Application.Features.Notifications.Commands.SendNotification;
using Quraaa.Application.Features.Notifications.Commands.SendTestNotification;
using Quraaa.Application.Features.Notifications.Common;
using System.Security.Claims;

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

        [HttpPost("send")]
        [ProducesResponseType(typeof(NotificationSendResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Send([FromBody] SendNotificationRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new
                {
                    type = "Unauthorized",
                    title = "Invalid Authentication Token",
                    detail = "The authentication token does not contain a valid user id."
                });
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

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("nameid")
                ?? User.FindFirstValue("sub");

            return Guid.TryParse(claimValue, out userId);
        }

        private bool IsTestEndpointEnabled()
        {
            return _environment.IsDevelopment()
                || _configuration.GetValue<bool>("Notifications:AllowTestEndpoint");
        }
    }
}
