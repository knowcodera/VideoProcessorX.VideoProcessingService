using Microsoft.EntityFrameworkCore;
using VideoProcessingService.Domain.Entities;

namespace VideoProcessingService.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Video>()
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId);

            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);
        }

        public DbSet<Video> Videos { get; set; }
        public DbSet<User> Users { get; set; }

    }
}
