using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Domain.Library;
using Quraaa.Domain.Shared.Exceptions;
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

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Two reset requests raced; the loser retries against the
                // winner's challenge.
                throw new ConflictException(
                    "The password reset changed concurrently. Retry the operation.");
            }
            catch (DbUpdateException exception) when (IsDuplicateChallengeViolation(exception))
            {
                // Both requests saw no challenge and inserted one. The winner's
                // code was mailed; surface the same retryable conflict rather
                // than a 500.
                throw new ConflictException(
                    "The password reset changed concurrently. Retry the operation.");
            }
        }

        private static bool IsDuplicateChallengeViolation(DbUpdateException exception) =>
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_LibraryPasswordResetChallenges_LibraryId"
            };
    }
}
