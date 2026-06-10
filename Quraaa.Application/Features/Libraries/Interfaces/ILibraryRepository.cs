using Quraaa.Domain.Library;

namespace Quraaa.Application.Features.Libraries.Interfaces
{
    public interface ILibraryRepository
    {
        Task AddLibraryAsync(LibraryAggregate library);
        Task SaveChangesAsync();
    }
}
