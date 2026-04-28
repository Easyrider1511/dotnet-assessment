using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Q5_ANPR.Data;
using Q5_ANPR.Models;

namespace Q5_ANPR.Services
{
    public class PlateReadRepository(AnprDbContext db)
    {
        /// <summary>
        /// Persists a plate read. Returns false if the file was already processed
        /// (unique constraint on SourceFileKey), so the caller can skip it.
        /// </summary>
        public async Task<bool> TrySaveAsync(PlateRead read)
        {
            db.PlateReads.Add(read);

            try
            {
                await db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                db.Entry(read).State = EntityState.Detached;

                if (await db.PlateReads.AnyAsync(p => p.SourceFileKey == read.SourceFileKey))
                    return false;

                throw;
            }
        }

        /// <summary>Retrieves plate reads within a date range, optionally filtered by camera name.</summary>
        public async Task<List<PlateRead>> QueryAsync(DateTime from, DateTime to, string? cameraName = null)
        {
            var query = db.PlateReads
                .Where(p => p.CapturedAt >= from && p.CapturedAt <= to);

            if (!string.IsNullOrWhiteSpace(cameraName))
                query = query.Where(p => p.CameraName == cameraName);

            return await query.OrderBy(p => p.CapturedAt).ToListAsync();
        }
    }
}
