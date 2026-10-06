using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quraaa.Application.Features.Admin.Interfaces;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Authors.Interfaces;
using Quraaa.Application.Features.BookReports.Interfaces;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Categories.Interfaces;
using Quraaa.Application.Features.DomainEvents.Interfaces;
using Quraaa.Application.Features.FavoriteBooks.Interfaces;
using Quraaa.Application.Features.Files.Interfaces;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Features.Payouts.Interfaces;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Application.Features.Reviews.Interfaces;
using Quraaa.Application.Shared.Persistence;
using Quraaa.Persistence.Interceptors;
using Microsoft.AspNetCore.Identity;
using Quraaa.Domain.Library;
using Quraaa.Persistence.Data;
using Quraaa.Persistence.Repositories;
using Quraaa.Persistence.Services;

namespace Quraaa.Persistence.Extensions
{
    public static class PersistenceDependencyInjectionHandler
    {
        /// <summary>
        /// Registers everything that talks to the database: the DbContext, its
        /// interceptors and unit of work, ASP.NET Core Identity with its EF stores,
        /// and the repositories. The host calls only this; it never configures EF Core or
        /// Identity itself.
        /// </summary>
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            AddDatabase(services, configuration);
            AddIdentityServices(services);
            AddRepositories(services);

            return services;
        }

        private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddScoped<DomainEventOutboxInterceptor>();

            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
                options.UseNpgsql(connectionString)
                    .AddInterceptors(
                        serviceProvider.GetRequiredService<DomainEventOutboxInterceptor>()));

            // Scoped like the DbContext it commits, so every repository in a request
            // stages into the same unit of work.
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        private static void AddIdentityServices(IServiceCollection services)
        {
            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.Password.RequiredLength = AuthenticationPasswordPolicy.MinimumLength;
                options.Password.RequiredUniqueChars = AuthenticationPasswordPolicy.RequiredUniqueCharacters;
                options.Password.RequireDigit = AuthenticationPasswordPolicy.RequireDigit;
                options.Password.RequireLowercase = AuthenticationPasswordPolicy.RequireLowercase;
                options.Password.RequireUppercase = AuthenticationPasswordPolicy.RequireUppercase;
                options.Password.RequireNonAlphanumeric = AuthenticationPasswordPolicy.RequireNonAlphanumeric;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IPasswordHasher<LibraryAggregate>, PasswordHasher<LibraryAggregate>>();
            services.AddScoped<ILibraryPasswordHasher, LibraryPasswordHasher>();
        }

        private static void AddRepositories(IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAuthorRepository, AuthorRepository>();
            services.AddScoped<ILibraryRepository, LibraryRepository>();
            services.AddScoped<ILibraryPasswordResetRepository, LibraryPasswordResetRepository>();
            services.AddScoped<ILibraryRegistrationRepository, LibraryRegistrationRepository>();
            services.AddScoped<ILibraryApprovalNotificationRepository, LibraryApprovalNotificationRepository>();
            services.AddScoped<IPushDeviceRepository, PushDeviceRepository>();
            services.AddScoped<IListingPushNotificationRepository, ListingPushNotificationRepository>();
            services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IBookReportRepository, BookReportRepository>();
            services.AddScoped<IBookVersionRepository, BookVersionRepository>();
            services.AddScoped<IBookModerationNotificationRepository, BookModerationNotificationRepository>();
            services.AddScoped<IBookReviewRepository, BookReviewRepository>();
            services.AddScoped<IFavoriteBookRepository, FavoriteBookRepository>();
            services.AddScoped<IBookPopularityRepository, BookPopularityRepository>();
            services.AddScoped<IHomeCatalogRepository, HomeCatalogRepository>();
            services.AddScoped<IListingRepository, ListingRepository>();
            services.AddScoped<IBookRepository, BookRepository>();
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<IBookPurchaseRepository, BookPurchaseRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<ISellerPayoutRepository, SellerPayoutRepository>();
            services.AddScoped<IPaymentEventInbox, PaymentEventInboxRepository>();
            services.AddScoped<IOrphanFileCandidateRepository, OrphanFileCandidateRepository>();
            services.AddScoped<IAdminDashboardRepository, AdminDashboardRepository>();
            services.AddScoped<IAdminModerationRepository, AdminModerationRepository>();
            services.AddScoped<IListingModerationRepository, ListingModerationRepository>();
        }
    }
}
