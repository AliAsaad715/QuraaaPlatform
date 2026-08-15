using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quraaa.Application.Features.Admin.Interfaces;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Authors.Interfaces;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Categories.Interfaces;
using Quraaa.Application.Features.Comments.Interfaces;
using Quraaa.Application.Features.FavoriteBooks.Interfaces;
using Quraaa.Application.Features.Files.Interfaces;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Features.Payouts.Interfaces;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Application.Features.Ratings.Interfaces;
using Quraaa.Persistence.Interceptors;
using Quraaa.Persistence.Repositories;
using Quraaa.Persistence.Services;

namespace Quraaa.Persistence.Extensions
{
    public static class PersistenceDependencyInjectionHandler
    {
        public static IServiceCollection AddPersistenceDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            var assembly = typeof(PersistenceDependencyInjectionHandler).Assembly;

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAuthorRepository, AuthorRepository>();
            services.AddScoped<ILibraryRepository, LibraryRepository>();
            services.AddScoped<ILibraryRegistrationRepository, LibraryRegistrationRepository>();
            services.AddScoped<ILibraryApprovalNotificationRepository, LibraryApprovalNotificationRepository>();
            services.AddScoped<IPushDeviceRepository, PushDeviceRepository>();
            services.AddScoped<IListingPushNotificationRepository, ListingPushNotificationRepository>();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IAuthenticationUnitOfWork, AuthenticationUnitOfWork>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<IBookRatingRepository, BookRatingRepository>();
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
            services.AddScoped<DomainEventOutboxInterceptor>();
            return services;
        }
    }
}
