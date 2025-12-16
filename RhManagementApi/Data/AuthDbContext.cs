
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RhManagementApi.Models;

namespace RhManagementApi.Data
{
    // Ensure your base class matches your Identity key types.
    // Example: if User : IdentityUser<int> and Role : IdentityRole<int>, use int key here.
    public class AuthDbContext : IdentityDbContext<User, Role, int>
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options)
            : base(options) { }

        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserRefreshToken>(entity =>
            {
                entity.ToTable("UserRefreshTokens");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Token)
                      .IsRequired()
                      .HasMaxLength(1024);

                entity.HasIndex(x => x.Token).IsUnique(); // one row per token
                entity.HasIndex(x => new { x.UserId, x.Revoked });

                entity.HasOne(x => x.User)
                      .WithMany() // or .WithMany(u => u.RefreshTokens) if you add collection
                      .HasForeignKey(x => x.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}