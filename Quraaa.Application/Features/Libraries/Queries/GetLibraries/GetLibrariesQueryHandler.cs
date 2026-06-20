using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Libraries.Queries.GetLibraries
{
    public class GetLibrariesQueryHandler : BaseApplicationService<GetLibrariesQueryHandler>, IRequestHandler<GetLibrariesQuery, AppResult<PagedResult<PublicLibraryResponse>>>
    {
        private readonly ILibraryRepository _libraryRepository;

        public GetLibrariesQueryHandler(
            ILibraryRepository libraryRepository,
            ILogger<GetLibrariesQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
        }

        public async Task<AppResult<PagedResult<PublicLibraryResponse>>> Handle(GetLibrariesQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var (libraries, totalCount) = await _libraryRepository.GetPagedAsync(
                    request.PageNumber,
                    request.PageSize,
                    request.SearchTerm,
                    cancellationToken);

                var items = libraries
                    .Select(l => new PublicLibraryResponse(
                        l.Id,
                        l.LibraryName,
                        l.Location,
                        l.LibraryImage,
                        l.HeaderImage,
                        l.Email))
                    .ToList();

                return new PagedResult<PublicLibraryResponse>(items, request.PageNumber, request.PageSize, totalCount);
            }, "Libraries retrieved successfully");
        }
    }
}