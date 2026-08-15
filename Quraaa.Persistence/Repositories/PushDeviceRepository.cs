using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Domain.User.Entities;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories;

public sealed class PushDeviceRepository : IPushDeviceRepository
{
    private const int MaximumDevicesPerUser = 10;

    private readonly ApplicationDbContext _context;

    public PushDeviceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task RegisterAsync(
        Guid userId,
        string token,
        CancellationToken cancellationToken = default)
    {
        var device = PushDevice.Create(userId, token);
        var lastModificationTime = DateTime.UtcNow;
        await using var transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            var affectedRows = await _context.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO "PushDevices"
                    ("Id", "UserId", "Token", "TokenHash", "CreationTime", "LastModificationTime")
                VALUES
                    ({{device.Id}}, {{device.UserId}}, {{device.Token}}, {{device.TokenHash}},
                     {{device.CreationTime}}, NULL)
                ON CONFLICT ("TokenHash") DO UPDATE
                SET "UserId" = EXCLUDED."UserId",
                    "Token" = EXCLUDED."Token",
                    "LastModificationTime" = {{lastModificationTime}}
                WHERE "PushDevices"."Token" = EXCLUDED."Token"
                """, cancellationToken);

            if (affectedRows != 1)
            {
                // A SHA-256 collision must never silently reassign a different raw
                // device token. Do not include either token in the exception or logs.
                throw new InvalidOperationException(
                    "The device token could not be registered safely.");
            }

            await PruneOldDevicesAsync(userId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task UnregisterAsync(
        Guid userId,
        string token,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = PushDevice.ComputeTokenHash(token);
        await Devices
            .Where(device =>
                device.UserId == userId
                && device.TokenHash == tokenHash
                && device.Token == token)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetTokensByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await Devices
            .AsNoTracking()
            .Where(device => device.UserId == userId)
            .OrderByDescending(device => device.LastModificationTime ?? device.CreationTime)
            .ThenByDescending(device => device.Id)
            .Take(MaximumDevicesPerUser)
            .Select(device => device.Token)
            .ToListAsync(cancellationToken);
    }

    public async Task RemoveTokensAsync(
        Guid userId,
        IReadOnlyCollection<string> tokens,
        CancellationToken cancellationToken = default)
    {
        if (tokens.Count == 0)
        {
            return;
        }

        var distinctTokens = tokens
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        foreach (var token in distinctTokens)
        {
            var tokenHash = PushDevice.ComputeTokenHash(token);
            await Devices
                .Where(device =>
                    device.UserId == userId
                    && device.TokenHash == tokenHash
                    && device.Token == token)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    private DbSet<PushDevice> Devices => _context.Set<PushDevice>();

    private async Task PruneOldDevicesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync($$"""
            WITH "OldDevices" AS
            (
                SELECT "Id", "LastModificationTime"
                FROM "PushDevices"
                WHERE "UserId" = {{userId}}
                ORDER BY COALESCE("LastModificationTime", "CreationTime") DESC, "Id" DESC
                OFFSET {{MaximumDevicesPerUser}}
            )
            DELETE FROM "PushDevices" AS device
            USING "OldDevices" AS old_device
            WHERE device."Id" = old_device."Id"
              AND device."UserId" = {{userId}}
              AND device."LastModificationTime"
                  IS NOT DISTINCT FROM old_device."LastModificationTime"
            """, cancellationToken);
    }
}
