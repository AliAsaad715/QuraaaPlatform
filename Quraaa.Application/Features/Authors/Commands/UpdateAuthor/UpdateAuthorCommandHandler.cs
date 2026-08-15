using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authors.Common;
using Quraaa.Application.Features.Authors.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Authors.Commands.UpdateAuthor
{
    public class UpdateAuthorCommandHandler
        : BaseApplicationService<UpdateAuthorCommandHandler>,
          IRequestHandler<UpdateAuthorCommand, AppResult<AuthorResponse>>
    {
        private readonly IAuthorRepository _authorRepository;

        public UpdateAuthorCommandHandler(
            IAuthorRepository authorRepository,
            ILogger<UpdateAuthorCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _authorRepository = authorRepository;
        }

        public async Task<AppResult<AuthorResponse>> Handle(UpdateAuthorCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync<UpdateAuthorCommand, AuthorResponse>(request, async () =>
            {
                var author = await _authorRepository.GetByIdAsync(request.Id, cancellationToken)
                    ?? throw new NotFoundException($"Author with ID {request.Id} was not found.");

                author.UpdateDetails(request.Name, request.Bio, request.PhotoUrl, request.BirthDate, request.ModifiedBy);

                await _authorRepository.SaveChangesAsync(cancellationToken);

                return new AuthorResponse(author.Id, author.Name, author.Bio, author.PhotoUrl, author.BirthDate, author.CreationTime);
            }, "Author updated successfully");
        }
    }
}
