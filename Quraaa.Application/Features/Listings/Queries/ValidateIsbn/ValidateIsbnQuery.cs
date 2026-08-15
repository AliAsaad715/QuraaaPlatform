using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Listings.Queries.ValidateIsbn
{
    public record ValidateIsbnQuery(string Isbn) : IRequest<AppResult<bool>>;
}
