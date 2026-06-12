using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Domain.Library;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class LibraryRepository : ILibraryRepository
    {
        private readonly ApplicationDbContext _context;

        public LibraryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddLibraryAsync(LibraryAggregate library)
        {
            await _context.Libraries.AddAsync(library);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
