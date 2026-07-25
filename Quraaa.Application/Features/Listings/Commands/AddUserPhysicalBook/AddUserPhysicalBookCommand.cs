using MediatR;
using Quraaa.Application.Features.Listings.Commands.AddPhysicalBook;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.Marketplace.Enums;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Listings.Commands.AddUserPhysicalBook
{
    public record AddUserPhysicalBookCommand : IRequest<AppResult<AddPhysicalBookResponse>>
    {
        [JsonIgnore]
        public Guid RequestingUserId { get; init; }

        public decimal Price { get; init; }
        public BookCondition Condition { get; init; }
        public string Isbn { get; init; } = null!;
    }
}