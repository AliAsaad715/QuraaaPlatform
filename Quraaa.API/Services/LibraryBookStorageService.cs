using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Files;

namespace Quraaa.API.Services
{
    // A digital listing's asset lives under the private storage root (outside
    // wwwroot) — never reachable via a direct URL. Delivery goes exclusively
    // through the authorized, ownership-checked download endpoint.
    public class LibraryBookStorageService : ILibraryBookStorageService
    {
        private readonly IFileStorageService _fileStorageService;

        public LibraryBookStorageService(IFileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }

        public Task<string> SaveAsync(IUploadedFile file, CancellationToken cancellationToken = default)
            => _fileStorageService.SaveAsync(file, "books", cancellationToken);
    }
}
