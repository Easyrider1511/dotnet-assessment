using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Q4_FileService.Controllers;
using Q4_FileService.Data;
using Q4_FileService.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Q4_FileService.Tests
{
    public class FilesControllerTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly Mock<BlobServiceClient> _blobServiceMock;
        private readonly FilesController _controller;

        public FilesControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(options);

            // Build mock chain: BlobServiceClient → BlobContainerClient → BlobClient
            var blobClientMock = new Mock<BlobClient>();
            blobClientMock.Setup(b => b.Uri)
                .Returns(new Uri("https://fake.blob.core.windows.net/uploads/test.jpg"));

            // Match UploadAsync(Stream, BlobUploadOptions, CancellationToken) — the overload used by the controller
            blobClientMock.Setup(b => b.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobUploadOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var containerMock = new Mock<BlobContainerClient>();
            containerMock.Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(blobClientMock.Object);
            containerMock.Setup(c => c.CreateIfNotExistsAsync(
                    It.IsAny<PublicAccessType>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<BlobContainerEncryptionScopeOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

            _blobServiceMock = new Mock<BlobServiceClient>();
            _blobServiceMock.Setup(s => s.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(containerMock.Object);

            _controller = new FilesController(_db, _blobServiceMock.Object);
        }

        public void Dispose() => _db.Dispose();

        // ── FileRecord model ──────────────────────────────────────────────────

        [Fact]
        public void FileRecord_DefaultValues_AreEmpty()
        {
            var record = new FileRecord();
            Assert.Equal(string.Empty, record.Name);
            Assert.Equal(string.Empty, record.ContentType);
            Assert.Equal(string.Empty, record.Extension);
            Assert.Equal(string.Empty, record.Location);
            Assert.Equal(string.Empty, record.BlobPath);
        }

        [Fact]
        public void FileRecord_PropertiesAssigned_Correctly()
        {
            var ts = DateTime.UtcNow;
            var record = new FileRecord
            {
                Name               = "photo",
                Size               = 1024,
                ContentType        = "image/jpeg",
                Extension          = ".jpg",
                Location           = "London",
                TimestampProcessed = ts,
                BlobPath           = "https://fake.blob/uploads/photo.jpg"
            };

            Assert.Equal("photo",          record.Name);
            Assert.Equal(1024,             record.Size);
            Assert.Equal("image/jpeg",     record.ContentType);
            Assert.Equal(".jpg",           record.Extension);
            Assert.Equal("London",         record.Location);
            Assert.Equal(ts,               record.TimestampProcessed);
            Assert.Equal("https://fake.blob/uploads/photo.jpg", record.BlobPath);
        }

        // ── Upload — validation ───────────────────────────────────────────────

        [Fact]
        public async Task Upload_NullFile_ReturnsBadRequest()
        {
            var result = await _controller.Upload(null!, null);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Upload_EmptyFile_ReturnsBadRequest()
        {
            var emptyFile = new FormFile(Stream.Null, 0, 0, "file", "empty.txt");
            var result = await _controller.Upload(emptyFile, null);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Upload_ValidFile_ReturnsCreatedAndPersistsRecord()
        {
            var content  = new MemoryStream("hello world"u8.ToArray());
            var formFile = new FormFile(content, 0, content.Length, "file", "photo.jpg")
            {
                Headers     = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };

            var result = await _controller.Upload(formFile, "London");

            var created = Assert.IsType<CreatedAtActionResult>(result);
            var record  = Assert.IsType<FileRecord>(created.Value);
            Assert.Equal("photo",       record.Name);
            Assert.Equal("image/jpeg",  record.ContentType);
            Assert.Equal(".jpg",        record.Extension);
            Assert.Equal("London",      record.Location);
            Assert.Equal(content.Length, record.Size);
            Assert.Equal(1, await _db.FileRecords.CountAsync());
        }

        [Fact]
        public async Task Upload_ValidFile_NoLocation_DefaultsToUnknown()
        {
            var content  = new MemoryStream("data"u8.ToArray());
            var formFile = new FormFile(content, 0, content.Length, "file", "doc.pdf")
            {
                Headers     = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var result = await _controller.Upload(formFile, null);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal("unknown", Assert.IsType<FileRecord>(created.Value).Location);
        }

        // ── GetById ───────────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_ExistingRecord_ReturnsOk()
        {
            var record = MakeRecord("doc");
            _db.FileRecords.Add(record);
            await _db.SaveChangesAsync();

            var result = await _controller.GetById(record.Id);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("doc", Assert.IsType<FileRecord>(ok.Value).Name);
        }

        [Fact]
        public async Task GetById_NonExistentId_ReturnsNotFound()
        {
            var result = await _controller.GetById(999);
            Assert.IsType<NotFoundResult>(result);
        }

        // ── GetAll ────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAll_EmptyDatabase_ReturnsEmptyList()
        {
            var result = await _controller.GetAll();
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Empty(Assert.IsAssignableFrom<IEnumerable<FileRecord>>(ok.Value));
        }

        [Fact]
        public async Task GetAll_MultipleRecords_OrderedByTimestampDescending()
        {
            var older = MakeRecord("old",  DateTime.UtcNow.AddHours(-2));
            var newer = MakeRecord("new",  DateTime.UtcNow);
            _db.FileRecords.AddRange(older, newer);
            await _db.SaveChangesAsync();

            var result = await _controller.GetAll();
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<FileRecord>>(ok.Value);
            Assert.Equal("new", System.Linq.Enumerable.First(list).Name);
        }

        // ── History — query filters ───────────────────────────────────────────

        [Fact]
        public async Task History_FilterByDateRange_ReturnsMatchingRecords()
        {
            var baseTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
            _db.FileRecords.AddRange(
                MakeRecord("inside",  baseTime,              "X"),
                MakeRecord("outside", baseTime.AddDays(10),  "X"));
            await _db.SaveChangesAsync();

            var result = await _controller.History(
                from: baseTime.AddHours(-1), to: baseTime.AddHours(1), location: null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var single = Assert.Single(Assert.IsAssignableFrom<IEnumerable<FileRecord>>(ok.Value));
            Assert.Equal("inside", single.Name);
        }

        [Fact]
        public async Task History_FilterByLocation_ReturnsMatchingRecords()
        {
            _db.FileRecords.AddRange(
                MakeRecord("london", DateTime.UtcNow, "London"),
                MakeRecord("madrid", DateTime.UtcNow, "Madrid"));
            await _db.SaveChangesAsync();

            var result = await _controller.History(from: null, to: null, location: "London");

            var ok = Assert.IsType<OkObjectResult>(result);
            var single = Assert.Single(Assert.IsAssignableFrom<IEnumerable<FileRecord>>(ok.Value));
            Assert.Equal("london", single.Name);
        }

        [Fact]
        public async Task History_NoFilters_ReturnsAllRecords()
        {
            _db.FileRecords.AddRange(MakeRecord("a"), MakeRecord("b"));
            await _db.SaveChangesAsync();

            var result = await _controller.History(from: null, to: null, location: null);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(2, System.Linq.Enumerable.Count(
                Assert.IsAssignableFrom<IEnumerable<FileRecord>>(ok.Value)));
        }

        // ── Blob storage failure ──────────────────────────────────────────────

        [Fact]
        public async Task Upload_BlobStorageUnavailable_Returns503()
        {
            // Arrange: make GetBlobContainerClient throw to simulate Azurite / Azure being down
            var failingBlob = new Mock<BlobServiceClient>();
            failingBlob.Setup(s => s.GetBlobContainerClient(It.IsAny<string>()))
                .Throws(new InvalidOperationException("Storage unavailable"));

            var controller = new FilesController(_db, failingBlob.Object);

            var content  = new MemoryStream("data"u8.ToArray());
            var formFile = new FormFile(content, 0, content.Length, "file", "test.jpg")
            {
                Headers     = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };

            var result = await controller.Upload(formFile, null);

            Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, ((ObjectResult)result).StatusCode);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static FileRecord MakeRecord(string name, DateTime? ts = null, string location = "X") =>
            new()
            {
                Name               = name,
                Size               = 512,
                ContentType        = "text/plain",
                Extension          = ".txt",
                Location           = location,
                TimestampProcessed = ts ?? DateTime.UtcNow,
                BlobPath           = $"https://fake.blob/{name}.txt"
            };
    }
}
