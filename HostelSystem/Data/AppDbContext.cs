using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HostelSystem.Pages.Complaints;

namespace HostelSystem.Data
{
    // IdentityDbContext replaces DbContext —
    // it includes all the Identity tables automatically
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<StatusHistory> StatusHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // This line is required when using IdentityDbContext
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<StatusHistory>()
                .HasOne(s => s.Complaint)
                .WithMany(c => c.StatusHistories)
                .HasForeignKey(s => s.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}