using Quraaa.Domain.Library;

namespace Quraaa.Application.Features.Libraries.Interfaces
{
    public interface ILibraryRepository
    {
        Task<bool> ExistsByUserIdAsync(Guid userId);
        Task AddLibraryAsync(LibraryAggregate library);
        Task SaveChangesAsync();
    }
}
