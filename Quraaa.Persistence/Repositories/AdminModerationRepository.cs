using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Admin.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Domain.Author;
using Quraaa.Domain.Library;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.User;
using Quraaa.Domain.User.Enums;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories;

public class AdminModerationRepository : IAdminModerationRepository
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminModerationRepository(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ───────────────────────────── reads ─────────────────────────────

    public async Task<(IReadOnlyCollection<AdminUserResponse> Items, int TotalCount)> GetUsersAsync(
        string? searchTerm,
        Role? role,
        bool includeDeactivated,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters so the admin can also see deactivated accounts;
        // the IsDeleted predicate below is what actually decides.
        var query = _context.UsersProfiles.AsNoTracking().IgnoreQueryFilters();

        if (!includeDeactivated)
        {
            query = query.Where(user => !user.IsDeleted);
        }

        if (role.HasValue)
        {
            query = query.Where(user => user.Role == role.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalized = searchTerm.Trim().ToLower();
            query = query.Where(user =>
                user.FirstName.ToLower().Contains(normalized)
                || user.LastName.ToLower().Contains(normalized)
                || user.PhoneNumber.Contains(normalized));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(user => user.CreationTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new AdminUserResponse(
                user.Id,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.Role,
                user.IsDeleted,
                user.DeleationTime,
                _context.Libraries
                    .IgnoreQueryFilters()
                    .Where(library => library.UserId == user.Id)
                    .Select(library => (Guid?)library.Id)
                    .FirstOrDefault(),
                _context.Libraries
                    .IgnoreQueryFilters()
                    .Where(library => library.UserId == user.Id)
                    .Select(library => library.LibraryName)
                    .FirstOrDefault(),
                user.CreationTime))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyCollection<AdminAuthorResponse> Items, int TotalCount)> GetAuthorsAsync(
        string? searchTerm,
        bool includeDeactivated,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Authors.AsNoTracking().IgnoreQueryFilters();

        if (!includeDeactivated)
        {
            query = query.Where(author => !author.IsDeleted);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalized = searchTerm.Trim().ToLower();
            query = query.Where(author => author.Name.ToLower().Contains(normalized));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(author => author.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(author => new AdminAuthorResponse(
                author.Id,
                author.Name,
                author.Bio,
                author.PhotoUrl,
                author.BirthDate,
                _context.Books.IgnoreQueryFilters().Count(book =>
                    book.AuthorId == author.Id && !book.IsDeleted),
                author.IsDeleted,
                author.DeleationTime,
                author.CreationTime))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyCollection<AdminLibraryResponse> Items, int TotalCount)> GetLibrariesAsync(
        string? searchTerm,
        bool includeDeactivated,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query =
            from library in _context.Libraries.AsNoTracking().IgnoreQueryFilters()
            join owner in _context.UsersProfiles.AsNoTracking().IgnoreQueryFilters()
                on library.UserId equals owner.Id
            select new { Library = library, Owner = owner };

        if (!includeDeactivated)
        {
            query = query.Where(row => !row.Library.IsDeleted);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalized = searchTerm.Trim().ToLower();
            query = query.Where(row =>
                row.Library.LibraryName.ToLower().Contains(normalized)
                || row.Library.Email.ToLower().Contains(normalized));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(row => row.Library.CreationTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(row => new AdminLibraryResponse(
                row.Library.Id,
                row.Library.LibraryName,
                row.Library.Email,
                row.Library.Location,
                row.Library.ApprovalStatus,
                row.Owner.Id,
                row.Owner.FirstName + " " + row.Owner.LastName,
                _context.Listings.IgnoreQueryFilters().Count(listing =>
                    listing.LibraryId == row.Library.Id && !listing.IsDeleted),
                row.Library.IsDeleted,
                row.Library.DeleationTime,
                row.Library.CreationTime))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    // ─────────────────────── deletion guards ───────────────────────
    // Only live rows block a delete: something already deactivated is on its
    // own way out and must not keep another record alive forever.

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<EntityDeletionBlocker>>> GetUserDeletionBlockersAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToArray();

        return await BuildBlockersAsync(
            ids,
            cancellationToken,
            ("Library", _context.Libraries.IgnoreQueryFilters()
                .Where(x => ids.Contains(x.UserId) && !x.IsDeleted)
                .Select(x => x.UserId)),
            ("Listings", _context.Listings.IgnoreQueryFilters()
                .Where(x => x.UserId != null && ids.Contains(x.UserId.Value) && !x.IsDeleted)
                .Select(x => x.UserId!.Value)),
            ("Orders", _context.Orders.IgnoreQueryFilters()
                .Where(x => ids.Contains(x.BuyerUserId) && !x.IsDeleted)
                .Select(x => x.BuyerUserId)),
            ("Purchases", _context.BookPurchases.IgnoreQueryFilters()
                .Where(x => ids.Contains(x.UserId) && !x.IsDeleted)
                .Select(x => x.UserId)),
            ("Ratings", _context.BookRatings.IgnoreQueryFilters()
                .Where(x => ids.Contains(x.UserId) && !x.IsDeleted)
                .Select(x => x.UserId)),
            ("Comments", _context.Comments.IgnoreQueryFilters()
                .Where(x => ids.Contains(x.UserId) && !x.IsDeleted)
                .Select(x => x.UserId)),
            ("Reports", _context.BookReports.IgnoreQueryFilters()
                .Where(x => ids.Contains(x.UserId) && !x.IsDeleted)
                .Select(x => x.UserId)),
            ("Favorites", _context.FavoriteBooks.IgnoreQueryFilters()
                .Where(x => ids.Contains(x.UserId) && !x.IsDeleted)
                .Select(x => x.UserId)));
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<EntityDeletionBlocker>>> GetAuthorDeletionBlockersAsync(
        IReadOnlyCollection<Guid> authorIds,
        CancellationToken cancellationToken = default)
    {
        var ids = authorIds.Distinct().ToArray();

        return await BuildBlockersAsync(
            ids,
            cancellationToken,
            ("Books", _context.Books.IgnoreQueryFilters()
                .Where(x => x.AuthorId != null && ids.Contains(x.AuthorId.Value) && !x.IsDeleted)
                .Select(x => x.AuthorId!.Value)));
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<EntityDeletionBlocker>>> GetLibraryDeletionBlockersAsync(
        IReadOnlyCollection<Guid> libraryIds,
        CancellationToken cancellationToken = default)
    {
        var ids = libraryIds.Distinct().ToArray();

        return await BuildBlockersAsync(
            ids,
            cancellationToken,
            ("Listings", _context.Listings.IgnoreQueryFilters()
                .Where(x => x.LibraryId != null && ids.Contains(x.LibraryId.Value) && !x.IsDeleted)
                .Select(x => x.LibraryId!.Value)),
            ("Payouts", _context.SellerPayouts.IgnoreQueryFilters()
                .Where(x => ids.Contains(x.LibraryId) && !x.IsDeleted)
                .Select(x => x.LibraryId)),
            ("Sold order items", _context.Orders.IgnoreQueryFilters()
                .SelectMany(order => order.Items)
                .Where(item => item.SellerType == SellerType.Library && ids.Contains(item.SellerId))
                .Select(item => item.SellerId)));
    }

    private static async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<EntityDeletionBlocker>>> BuildBlockersAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken,
        params (string Reference, IQueryable<Guid> Owners)[] sources)
    {
        var blockers = ids.ToDictionary(id => id, _ => new List<EntityDeletionBlocker>());

        foreach (var (reference, owners) in sources)
        {
            var counts = await owners
                .GroupBy(ownerId => ownerId)
                .Select(group => new { OwnerId = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken);

            foreach (var row in counts)
            {
                if (blockers.TryGetValue(row.OwnerId, out var list))
                {
                    list.Add(new EntityDeletionBlocker(reference, row.Count));
                }
            }
        }

        return blockers.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyCollection<EntityDeletionBlocker>)pair.Value);
    }

    // ───────────────────────── lifecycle ─────────────────────────

    public Task<int> CountSuperAdminsAsync(CancellationToken cancellationToken = default) =>
        _context.UsersProfiles
            .AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync(user => user.Role == Role.SuperAdmin && !user.IsDeleted, cancellationToken);

    public async Task<IReadOnlyCollection<UserAggregate>> GetUsersByIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default) =>
        await _context.UsersProfiles
            .IgnoreQueryFilters()
            .Where(user => userIds.Contains(user.Id))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<AuthorAggregate>> GetAuthorsByIdsAsync(
        IReadOnlyCollection<Guid> authorIds,
        CancellationToken cancellationToken = default) =>
        await _context.Authors
            .IgnoreQueryFilters()
            .Where(author => authorIds.Contains(author.Id))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<LibraryAggregate>> GetLibrariesByIdsAsync(
        IReadOnlyCollection<Guid> libraryIds,
        CancellationToken cancellationToken = default) =>
        await _context.Libraries
            .IgnoreQueryFilters()
            .Where(library => libraryIds.Contains(library.Id))
            .ToListAsync(cancellationToken);

    public async Task RemoveUsersAsync(
        IReadOnlyCollection<UserAggregate> users,
        CancellationToken cancellationToken = default)
    {
        _context.UsersProfiles.RemoveRange(users);

        // The sign-in identity lives in its own table keyed by the same id;
        // leaving it behind would keep the phone number reserved.
        var ids = users.Select(user => user.Id).ToArray();

        var identities = await _context.Users
            .Where(identity => ids.Contains(identity.Id))
            .ToListAsync(cancellationToken);

        foreach (var identity in identities)
        {
            await _userManager.DeleteAsync(identity);
        }
    }

    public void RemoveAuthors(IReadOnlyCollection<AuthorAggregate> authors) =>
        _context.Authors.RemoveRange(authors);

    public void RemoveLibraries(IReadOnlyCollection<LibraryAggregate> libraries) =>
        _context.Libraries.RemoveRange(libraries);

    public async Task<AdminUserResponse> CreateSuperAdminAsync(
        string phoneNumber,
        string password,
        string firstName,
        string lastName,
        Guid createdBy,
        CancellationToken cancellationToken = default)
    {
        var existing = await _userManager.FindByNameAsync(phoneNumber);
        if (existing is not null)
        {
            throw new ConflictException("An account already uses this phone number.");
        }

        var identityUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = phoneNumber,
            PhoneNumber = phoneNumber,
            PhoneNumberConfirmed = true,
            Email = $"{phoneNumber}@quraaa.com",
            EmailConfirmed = true,
        };

        var createResult = await _userManager.CreateAsync(identityUser, password);
        if (!createResult.Succeeded)
        {
            throw new ApplicationBusinessException(
                string.Join("; ", createResult.Errors.Select(error => error.Description)),
                "Password");
        }

        // Both roles: SuperAdmin carries the extra authority, Admin keeps every
        // ordinary administrator endpoint working for them.
        await _userManager.AddToRolesAsync(
            identityUser,
            [Role.Admin.ToString(), Role.SuperAdmin.ToString()]);

        var profile = new UserAggregate(
            identityUser.Id,
            firstName,
            lastName,
            phoneNumber,
            identityUser.PasswordHash!,
            Gender.Male,
            Role.SuperAdmin,
            new DateOnly(2000, 1, 1));

        profile.UpdateAudit(createdBy);

        await _context.UsersProfiles.AddAsync(profile, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new AdminUserResponse(
            profile.Id,
            profile.FirstName,
            profile.LastName,
            profile.PhoneNumber,
            profile.Role,
            IsDeactivated: false,
            DeactivatedAtUtc: null,
            LibraryId: null,
            LibraryName: null,
            profile.CreationTime);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
