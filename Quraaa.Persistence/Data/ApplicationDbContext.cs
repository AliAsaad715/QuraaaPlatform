using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Cart;
using Quraaa.Domain.Category;
using Quraaa.Domain.Favorites;
using Quraaa.Domain.Library;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Orders;
using Quraaa.Domain.Purchases;
using Quraaa.Domain.Ratings;
using Quraaa.Domain.User;
using System.Reflection;

namespace Quraaa.Persistence.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<UserAggregate> UsersProfiles { get; set; }
        public DbSet<LibraryAggregate> Libraries { get; set; }
        public DbSet<BookAggregate> Books { get; set; }
        public DbSet<ListingAggregate> Listings { get; set; }
        public DbSet<CategoryAggregate> Categories { get; set; }
        public DbSet<FavoriteBookAggregate> FavoriteBooks { get; set; }
        public DbSet<BookPurchaseAggregate> BookPurchases { get; set; }
        public DbSet<BookRatingAggregate> BookRatings { get; set; }
        public DbSet<CartAggregate> Carts { get; set; }
        public DbSet<OrderAggregate> Orders { get; set; }
        public DbSet<ProcessedPaymentEvent> ProcessedPaymentEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            modelBuilder.Entity<CategoryAggregate>().HasQueryFilter(c => c.IsActive == true);
        }
    }
}
