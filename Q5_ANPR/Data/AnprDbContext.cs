using Microsoft.EntityFrameworkCore;
using Q5_ANPR.Models;

namespace Q5_ANPR.Data
{
    public class AnprDbContext(DbContextOptions<AnprDbContext> options) : DbContext(options)
    {
        public DbSet<PlateRead> PlateReads => Set<PlateRead>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Unique index prevents the same file from being processed twice even
            // when different cameras produce files with identical names.
            modelBuilder.Entity<PlateRead>()
                .HasIndex(p => p.SourceFileKey)
                .IsUnique();

            // Composite index for the primary query pattern: date range + camera name
            modelBuilder.Entity<PlateRead>()
                .HasIndex(p => new { p.CameraName, p.CapturedAt });
        }
    }
}
