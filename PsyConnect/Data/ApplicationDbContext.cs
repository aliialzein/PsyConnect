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
        public DbSet<EmailOTPCode> EmailOTPCodes { get; set; }
        public DbSet<PsyConnect.Models.RoleVM> RoleVM { get; set; } = default!;
        public DbSet<PsyConnect.Models.AssignVM> AssignVM { get; set; } = default!;
    }
}
