using System;
using System.ComponentModel.DataAnnotations;

namespace Q4_FileService.Models
{
    public class FileRecord
    {
        public int Id { get; set; }

        [Required, MaxLength(260)]
        public string Name { get; set; } = string.Empty;

        public long Size { get; set; }

        [MaxLength(200)]
        public string ContentType { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Extension { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Location { get; set; } = string.Empty;

        public DateTime TimestampProcessed { get; set; }

        [MaxLength(1000)]
        public string BlobPath { get; set; } = string.Empty;
    }
}
