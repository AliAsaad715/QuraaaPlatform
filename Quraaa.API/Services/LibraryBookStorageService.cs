using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Files;

namespace Quraaa.API.Services
{
    // A digital listing's asset is stored as a private provider object, never as a
    // public URL. Delivery goes through the authorized, ownership-checked endpoint.
    public class LibraryBookStorageService : ILibraryBookStorageService
    {
        private readonly IFileStorageService _fileStorageService;

        public LibraryBookStorageService(IFileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }

        public Task<string> SaveAsync(IUploadedFile file, CancellationToken cancellationToken = default)
            => _fileStorageService.SaveAsync(file, "books", cancellationToken);

        public Task DeleteAsync(string? storedReference, CancellationToken cancellationToken = default) =>
            string.IsNullOrWhiteSpace(storedReference)
                ? Task.CompletedTask
                : _fileStorageService.DeleteAsync(storedReference, cancellationToken);
    }
}
