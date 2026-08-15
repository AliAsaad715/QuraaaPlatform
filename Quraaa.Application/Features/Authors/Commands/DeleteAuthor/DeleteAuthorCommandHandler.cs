using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authors.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Authors.Commands.DeleteAuthor
{
    public class DeleteAuthorCommandHandler
        : BaseApplicationService<DeleteAuthorCommandHandler>,
          IRequestHandler<DeleteAuthorCommand, AppResult>
    {
        private readonly IAuthorRepository _authorRepository;

        public DeleteAuthorCommandHandler(
            IAuthorRepository authorRepository,
            ILogger<DeleteAuthorCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _authorRepository = authorRepository;
        }

        public async Task<AppResult> Handle(DeleteAuthorCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var author = await _authorRepository.GetByIdAsync(request.Id, cancellationToken)
                    ?? throw new NotFoundException($"Author with ID {request.Id} was not found.");

                await _authorRepository.RemoveAsync(author, cancellationToken);
            }, "Author deleted successfully");
        }
    }
}
