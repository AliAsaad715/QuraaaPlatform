using MediatR;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Notifications.Commands.RegisterDeviceToken
{
    public record RegisterDeviceTokenCommand : IRequest<AppResult>
    {
        [JsonIgnore]
        public Guid RequestingUserId { get; init; }

        public string DeviceToken { get; init; } = null!;
    }
}
