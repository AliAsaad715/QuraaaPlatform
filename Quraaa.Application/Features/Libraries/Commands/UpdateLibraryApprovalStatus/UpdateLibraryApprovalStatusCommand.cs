using MediatR;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.Library.Enums;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Libraries.Commands.UpdateLibraryApprovalStatus
{
    public record UpdateLibraryApprovalStatusCommand : IRequest<AppResult>
    {
        [JsonIgnore]
        public Guid LibraryId { get; init; }

        [JsonIgnore]
        public Guid AdminId { get; init; }

        public LibraryApprovalStatus Status { get; init; }
    }
}