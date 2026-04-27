using Microsoft.EntityFrameworkCore;
using Q4_FileService.Models;

namespace Q4_FileService.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<FileRecord> FileRecords => Set<FileRecord>();
    }
}
