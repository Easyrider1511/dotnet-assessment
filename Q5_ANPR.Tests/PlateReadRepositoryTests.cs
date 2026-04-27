using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Q5_ANPR.Data;
using Q5_ANPR.Models;
using Q5_ANPR.Services;

namespace Q5_ANPR.Tests
{
    public class PlateReadRepositoryTests : IDisposable
    {
        private readonly AnprDbContext _db;
        private readonly PlateReadRepository _repo;

        public PlateReadRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AnprDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new AnprDbContext(options);
            _repo = new PlateReadRepository(_db);
        }

        public void Dispose() => _db.Dispose();

        private static PlateRead MakePlateRead(string sourceKey = "Camera1/plate001.lpr") => new()
        {
            CountryOfVehicle = "GBZ",
            RegNumber        = "9112A",
            ConfidenceLevel  = 77,
            CameraName       = "GIBEXIT2",
            CapturedAt       = new DateTime(2014, 8, 27, 12, 10, 0),
            ImageFilename    = "27082014,12140198,9112A,77.jpg",
            SourceFileKey    = sourceKey
        };

        // ── TrySaveAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task TrySaveAsync_NewRecord_ReturnsTrueAndPersists()
        {
            var read = MakePlateRead();
            bool saved = await _repo.TrySaveAsync(read);

            Assert.True(saved);
            Assert.Equal(1, await _db.PlateReads.CountAsync());
        }

        [Fact]
        public async Task TrySaveAsync_DuplicateSourceKey_ReturnsFalseAndDoesNotDuplicate()
        {
            var read1 = MakePlateRead("Camera1/plate001.lpr");
            var read2 = MakePlateRead("Camera1/plate001.lpr"); // same key

            await _repo.TrySaveAsync(read1);
            bool savedAgain = await _repo.TrySaveAsync(read2);

            Assert.False(savedAgain);
            Assert.Equal(1, await _db.PlateReads.CountAsync());
        }

        [Fact]
        public async Task TrySaveAsync_SameFilename_DifferentCamera_BothSaved()
        {
            // Key difference: different camera folder → different SourceFileKey
            var cam1 = MakePlateRead("Camera1/plate001.lpr");
            var cam2 = MakePlateRead("Camera2/plate001.lpr");

            bool s1 = await _repo.TrySaveAsync(cam1);
            bool s2 = await _repo.TrySaveAsync(cam2);

            Assert.True(s1);
            Assert.True(s2);
            Assert.Equal(2, await _db.PlateReads.CountAsync());
        }

        // ── QueryAsync — date range ───────────────────────────────────────────

        [Fact]
        public async Task QueryAsync_DateRange_ReturnsOnlyRecordsInRange()
        {
            var inside  = MakePlateRead("cam/inside.lpr");
            inside.CapturedAt = new DateTime(2024, 6, 15);

            var outside = MakePlateRead("cam/outside.lpr");
            outside.CapturedAt = new DateTime(2024, 12, 1);

            await _repo.TrySaveAsync(inside);
            await _repo.TrySaveAsync(outside);

            var results = await _repo.QueryAsync(
                from: new DateTime(2024, 1, 1),
                to:   new DateTime(2024, 7, 1));

            Assert.Single(results);
            Assert.Equal("cam/inside.lpr", results[0].SourceFileKey);
        }

        [Fact]
        public async Task QueryAsync_InclusiveBoundaries_IncludesExactBoundaryRecords()
        {
            var boundary = MakePlateRead("cam/boundary.lpr");
            boundary.CapturedAt = new DateTime(2024, 6, 1, 0, 0, 0);

            await _repo.TrySaveAsync(boundary);

            var results = await _repo.QueryAsync(
                from: new DateTime(2024, 6, 1, 0, 0, 0),
                to:   new DateTime(2024, 6, 1, 0, 0, 0));

            Assert.Single(results);
        }

        // ── QueryAsync — camera filter ────────────────────────────────────────

        [Fact]
        public async Task QueryAsync_FilterByCamera_ReturnsOnlyMatchingCamera()
        {
            var cam1 = MakePlateRead("Camera1/a.lpr");
            cam1.CameraName = "GIBEXIT1";

            var cam2 = MakePlateRead("Camera2/b.lpr");
            cam2.CameraName = "GIBEXIT2";

            await _repo.TrySaveAsync(cam1);
            await _repo.TrySaveAsync(cam2);

            var results = await _repo.QueryAsync(
                from: DateTime.MinValue,
                to:   DateTime.MaxValue,
                cameraName: "GIBEXIT1");

            Assert.Single(results);
            Assert.Equal("GIBEXIT1", results[0].CameraName);
        }

        [Fact]
        public async Task QueryAsync_NoCameraFilter_ReturnsAllInRange()
        {
            await _repo.TrySaveAsync(MakePlateRead("cam/a.lpr"));
            await _repo.TrySaveAsync(MakePlateRead("cam/b.lpr"));

            var results = await _repo.QueryAsync(DateTime.MinValue, DateTime.MaxValue);

            Assert.Equal(2, results.Count);
        }

        // ── QueryAsync — ordering ─────────────────────────────────────────────

        [Fact]
        public async Task QueryAsync_MultipleRecords_ReturnedOrderedByCapturedAt()
        {
            var later   = MakePlateRead("cam/later.lpr");
            later.CapturedAt = new DateTime(2024, 8, 1);

            var earlier = MakePlateRead("cam/earlier.lpr");
            earlier.CapturedAt = new DateTime(2024, 1, 1);

            await _repo.TrySaveAsync(later);
            await _repo.TrySaveAsync(earlier);

            var results = await _repo.QueryAsync(DateTime.MinValue, DateTime.MaxValue);

            Assert.Equal("cam/earlier.lpr", results[0].SourceFileKey);
            Assert.Equal("cam/later.lpr",   results[1].SourceFileKey);
        }

        // ── Empty results ─────────────────────────────────────────────────────

        [Fact]
        public async Task QueryAsync_NoMatchingRecords_ReturnsEmptyList()
        {
            var results = await _repo.QueryAsync(DateTime.MinValue, DateTime.MaxValue);
            Assert.Empty(results);
        }
    }
}
