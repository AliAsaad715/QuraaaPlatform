using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Domain.Library;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class LibraryPasswordResetRepository : ILibraryPasswordResetRepository
    {
        private readonly ApplicationDbContext _context;

        public LibraryPasswordResetRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LibraryPasswordResetChallenge?> GetByLibraryIdAsync(
            Guid libraryId,
            CancellationToken cancellationToken = default)
        {
            return await _context.LibraryPasswordResetChallenges
                .FirstOrDefaultAsync(
                    challenge => challenge.LibraryId == libraryId,
                    cancellationToken);
        }

        public async Task AddAsync(
            LibraryPasswordResetChallenge challenge,
            CancellationToken cancellationToken = default)
        {
            await _context.LibraryPasswordResetChallenges.AddAsync(challenge, cancellationToken);
        }
    }
}
