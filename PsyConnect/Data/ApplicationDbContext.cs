using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PsyConnect.Models;

namespace PsyConnect.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; } = default!;
        public DbSet<EmailOTPCode> EmailOTPCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Booking (1) <-> (1) Review
            builder.Entity<Booking>()
                .HasOne(b => b.Review)
                .WithOne(r => r.Booking)
                .HasForeignKey<Review>(r => r.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // enforce one review per booking at DB level
            builder.Entity<Review>()
                .HasIndex(r => r.BookingId)
                .IsUnique();

            builder.Entity<Review>()
                .Property(r => r.UserId)
                .HasColumnType("nvarchar(450)");
        }
    }
}
