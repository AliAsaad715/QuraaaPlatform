using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Domain.Library;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public sealed class LibraryRegistrationRepository : ILibraryRegistrationRepository
    {
        private readonly ApplicationDbContext _context;

        public LibraryRegistrationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<LibraryRegistrationSession?> GetSessionByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            _context.LibraryRegistrationSessions.SingleOrDefaultAsync(
                session => session.UserId == userId,
                cancellationToken);

        public Task<LibraryRegistrationSession?> GetSessionByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
            {
                return Task.FromResult<LibraryRegistrationSession?>(null);
            }

            var normalizedTokenHash = tokenHash.Trim();
            return _context.LibraryRegistrationSessions.SingleOrDefaultAsync(
                session => session.TokenHash == normalizedTokenHash,
                cancellationToken);
        }

        public Task<LibraryEmailVerificationChallenge?> GetChallengeByLibraryIdAsync(
            Guid libraryId,
            CancellationToken cancellationToken = default) =>
            _context.LibraryEmailVerificationChallenges.SingleOrDefaultAsync(
                challenge => challenge.LibraryId == libraryId,
                cancellationToken);

        public async Task AddSessionAsync(
            LibraryRegistrationSession session,
            CancellationToken cancellationToken = default) =>
            await _context.LibraryRegistrationSessions.AddAsync(session, cancellationToken);

        public async Task AddChallengeAsync(
            LibraryEmailVerificationChallenge challenge,
            CancellationToken cancellationToken = default) =>
            await _context.LibraryEmailVerificationChallenges.AddAsync(challenge, cancellationToken);

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(
                    "The library registration changed concurrently. Reload it and retry.");
            }
            catch (DbUpdateException exception) when (IsRegistrationUniqueViolation(exception))
            {
                throw new ConflictException(
                    "A library registration session or email challenge already exists.");
            }
        }

        private static bool IsRegistrationUniqueViolation(DbUpdateException exception) =>
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_LibraryRegistrationSessions_UserId" or
                    "IX_LibraryRegistrationSessions_TokenHash" or
                    "IX_LibraryEmailVerificationChallenges_LibraryId"
            };
    }
}
