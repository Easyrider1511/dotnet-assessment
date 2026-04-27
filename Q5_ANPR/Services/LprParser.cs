using System;
using System.Globalization;
using System.IO;
using Q5_ANPR.Models;

namespace Q5_ANPR.Services
{
    // LPR file format (fields separated by \ or /):
    //   CountryOfVehicle\RegNumber\ConfidenceLevel\CameraName\Date\Time/ImageFilename
    // Example: NONE\r9112A\r77\rGIBEXIT2\20140827\1210/w27082014,12140198,9112A,77.jpg
    // Leading 'r' or 'w' on certain fields is dropped per the ACS spec.
    public static class LprParser
    {
        public static PlateRead Parse(string filePath, string fileContent)
        {
            // Normalise both delimiters to a single separator
            var raw   = fileContent.Trim().Replace('/', '\\');
            var parts = raw.Split('\\', StringSplitOptions.None);

            if (parts.Length < 7)
                throw new FormatException($"LPR file has {parts.Length} fields; expected 7. File: {filePath}");

            var country       = parts[0].TrimStart('r', 'w');
            var regNumber     = parts[1].TrimStart('r', 'w');
            var confidenceRaw = parts[2].TrimStart('r', 'w');
            var cameraName    = parts[3].TrimStart('r', 'w');
            var datePart      = parts[4];   // yyyyMMdd
            var timePart      = parts[5];   // HHmm
            var imageFilename = parts[6].TrimStart('r', 'w');

            if (!int.TryParse(confidenceRaw, out int confidence))
                confidence = 0;

            if (!DateTime.TryParseExact(datePart + timePart, "yyyyMMddHHmm",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime capturedAt))
            {
                // Fall back to the file's last-write time when the embedded timestamp is malformed
                capturedAt = File.GetLastWriteTime(filePath);
            }

            // GetFullPath normalises the path so two cameras sharing the same filename
            // still produce distinct keys (different directory → different full path).
            var sourceFileKey = Path.GetFullPath(filePath);

            return new PlateRead
            {
                CountryOfVehicle = country,
                RegNumber        = regNumber,
                ConfidenceLevel  = confidence,
                CameraName       = cameraName,
                CapturedAt       = capturedAt,
                ImageFilename    = imageFilename,
                SourceFileKey    = sourceFileKey
            };
        }
    }
}
