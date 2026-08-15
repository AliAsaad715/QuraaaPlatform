using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authors.Common;
using Quraaa.Application.Features.Authors.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Author;

namespace Quraaa.Application.Features.Authors.Commands.CreateAuthor
{
    public class CreateAuthorCommandHandler
        : BaseApplicationService<CreateAuthorCommandHandler>,
          IRequestHandler<CreateAuthorCommand, AppResult<AuthorResponse>>
    {
        private readonly IAuthorRepository _authorRepository;

        public CreateAuthorCommandHandler(
            IAuthorRepository authorRepository,
            ILogger<CreateAuthorCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _authorRepository = authorRepository;
        }

        public async Task<AppResult<AuthorResponse>> Handle(CreateAuthorCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync<CreateAuthorCommand, AuthorResponse>(request, async () =>
            {
                var author = new AuthorAggregate(
                    Guid.NewGuid(),
                    request.Name,
                    request.Bio,
                    request.PhotoUrl,
                    request.BirthDate
                );

                await _authorRepository.AddAsync(author, cancellationToken);

                return new AuthorResponse(author.Id, author.Name, author.Bio, author.PhotoUrl, author.BirthDate, author.CreationTime);
            }, "Author created successfully");
        }
    }
}
