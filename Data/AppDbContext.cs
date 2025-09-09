using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TanuiApp.Models;

namespace TanuiApp.Data
{
    public class AppDbContext : IdentityDbContext<Users>
    {
        public AppDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; } // ✅ Added WishlistItems

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // CartItem relationships
            builder.Entity<CartItem>()
                .HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CartItem>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ WishlistItem relationships
            builder.Entity<WishlistItem>()
                .HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<WishlistItem>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ Prevent duplicate wishlist items for same user/product
            builder.Entity<WishlistItem>()
                .HasIndex(w => new { w.UserId, w.ProductId })
                .IsUnique();
        }
    }
}