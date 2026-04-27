using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Q4_FileService.Data;
using Q4_FileService.Models;

namespace Q4_FileService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController(AppDbContext db, BlobServiceClient blobService) : ControllerBase
    {
        private const string ContainerName = "uploads";

        [HttpPost("upload")]
        [RequestSizeLimit(500 * 1024 * 1024)] // 500 MB
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] string? location)
        {
            if (file is null || file.Length == 0)
                return BadRequest("No file provided.");

            string blobPath;
            try
            {
                var container = blobService.GetBlobContainerClient(ContainerName);
                await container.CreateIfNotExistsAsync(PublicAccessType.None);

                var blobName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var blobClient = container.GetBlobClient(blobName);

                await using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
                });

                blobPath = blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                return StatusCode(503, $"Blob storage unavailable: {ex.Message}");
            }

            var record = new FileRecord
            {
                Name               = Path.GetFileNameWithoutExtension(file.FileName),
                Size               = file.Length,
                ContentType        = file.ContentType ?? "application/octet-stream",
                Extension          = Path.GetExtension(file.FileName),
                Location           = location ?? "unknown",
                TimestampProcessed = DateTime.UtcNow,
                BlobPath           = blobPath
            };

            db.FileRecords.Add(record);
            await db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var record = await db.FileRecords.FindAsync(id);
            return record is null ? NotFound() : Ok(record);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await db.FileRecords.OrderByDescending(f => f.TimestampProcessed).ToListAsync());

        [HttpGet("history")]
        public async Task<IActionResult> History(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? location)
        {
            var query = db.FileRecords.AsQueryable();

            if (from.HasValue)  query = query.Where(f => f.TimestampProcessed >= from.Value);
            if (to.HasValue)    query = query.Where(f => f.TimestampProcessed <= to.Value);
            if (!string.IsNullOrWhiteSpace(location))
                query = query.Where(f => f.Location.Contains(location));

            return Ok(await query.OrderByDescending(f => f.TimestampProcessed).ToListAsync());
        }
    }
}
