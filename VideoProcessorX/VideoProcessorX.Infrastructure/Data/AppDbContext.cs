using Microsoft.EntityFrameworkCore;
using VideoProcessingService.Domain.Entities;

namespace VideoProcessingService.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Video> Videos { get; set; }

    }
}
