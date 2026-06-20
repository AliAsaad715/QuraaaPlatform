using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Libraries.Commands.AddPhysicalBook
{
    public record AddPhysicalBookCommand(
        string? Isbn = null,
        string? Title = null,
        string? Author = null,
        int? PublicationYear = null
    ) : IRequest<AppResult>;
}
