using System;
using System.ComponentModel.DataAnnotations;

namespace Q5_ANPR.Models
{
    public class PlateRead
    {
        public int Id { get; set; }

        [Required, MaxLength(10)]
        public string CountryOfVehicle { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string RegNumber { get; set; } = string.Empty;

        public int ConfidenceLevel { get; set; }

        [Required, MaxLength(100)]
        public string CameraName { get; set; } = string.Empty;

        public DateTime CapturedAt { get; set; }

        [MaxLength(260)]
        public string ImageFilename { get; set; } = string.Empty;

        // Full normalised path (via Path.GetFullPath) — cameras sharing a filename still get distinct keys
        [Required, MaxLength(600)]
        public string SourceFileKey { get; set; } = string.Empty;
    }
}
