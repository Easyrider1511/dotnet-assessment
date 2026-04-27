using System;
using Xunit;
using Q5_ANPR.Services;

namespace Q5_ANPR.Tests
{
    public class LprParserTests
    {
        // Spec example from the assessment
        private const string SpecContent = @"NONE\r9112A\r77\rGIBEXIT2\20140827\1210/w27082014,12140198,9112A,77.jpg";
        private const string FakePath    = @"C:\ANPR\Camera1\plate001.lpr";

        // ── Spec example ─────────────────────────────────────────────────────

        [Fact]
        public void Parse_SpecExample_CountryOfVehicle()
        {
            var result = LprParser.Parse(FakePath, SpecContent);
            Assert.Equal("NONE", result.CountryOfVehicle);
        }

        [Fact]
        public void Parse_SpecExample_RegNumberStripsLeadingR()
        {
            var result = LprParser.Parse(FakePath, SpecContent);
            Assert.Equal("9112A", result.RegNumber);
        }

        [Fact]
        public void Parse_SpecExample_ConfidenceLevel()
        {
            var result = LprParser.Parse(FakePath, SpecContent);
            Assert.Equal(77, result.ConfidenceLevel);
        }

        [Fact]
        public void Parse_SpecExample_CameraNameStripsLeadingR()
        {
            var result = LprParser.Parse(FakePath, SpecContent);
            Assert.Equal("GIBEXIT2", result.CameraName);
        }

        [Fact]
        public void Parse_SpecExample_CapturedAtDate()
        {
            var result = LprParser.Parse(FakePath, SpecContent);
            Assert.Equal(new DateTime(2014, 8, 27, 12, 10, 0), result.CapturedAt);
        }

        [Fact]
        public void Parse_SpecExample_ImageFilenameStripsLeadingW()
        {
            var result = LprParser.Parse(FakePath, SpecContent);
            Assert.Equal("27082014,12140198,9112A,77.jpg", result.ImageFilename);
        }

        // ── SourceFileKey uniqueness ──────────────────────────────────────────

        [Fact]
        public void Parse_SourceFileKey_IncludesFullPath()
        {
            var result = LprParser.Parse(FakePath, SpecContent);
            Assert.Contains("plate001.lpr", result.SourceFileKey);
            Assert.Contains("Camera1", result.SourceFileKey);
        }

        [Fact]
        public void Parse_SameFilename_DifferentCamera_DifferentSourceKey()
        {
            const string cam1 = @"C:\ANPR\Camera1\read.lpr";
            const string cam2 = @"C:\ANPR\Camera2\read.lpr";

            var r1 = LprParser.Parse(cam1, SpecContent);
            var r2 = LprParser.Parse(cam2, SpecContent);

            Assert.NotEqual(r1.SourceFileKey, r2.SourceFileKey);
        }

        // ── Delimiter variants ───────────────────────────────────────────────

        [Fact]
        public void Parse_ForwardSlashDelimiters_ParsedCorrectly()
        {
            // All forward slashes instead of backslashes
            const string content = "GBZ/r9112A/r77/rGIBEXIT2/20140827/1210/w27082014,12140198,9112A,77.jpg";
            var result = LprParser.Parse(FakePath, content);

            Assert.Equal("GBZ",     result.CountryOfVehicle);
            Assert.Equal("9112A",   result.RegNumber);
            Assert.Equal(77,        result.ConfidenceLevel);
            Assert.Equal("GIBEXIT2",result.CameraName);
        }

        // ── Leading whitespace / trim ─────────────────────────────────────────

        [Fact]
        public void Parse_ContentWithLeadingAndTrailingWhitespace_ParsedCorrectly()
        {
            var content = "  " + SpecContent + "  ";
            var result = LprParser.Parse(FakePath, content);
            Assert.Equal("9112A", result.RegNumber);
        }

        // ── Malformed input ───────────────────────────────────────────────────

        [Fact]
        public void Parse_TooFewFields_ThrowsFormatException()
        {
            const string bad = @"NONE\r9112A\r77";
            Assert.Throws<FormatException>(() => LprParser.Parse(FakePath, bad));
        }

        [Fact]
        public void Parse_InvalidConfidence_DefaultsToZero()
        {
            const string content = @"NONE\r9112A\rXXX\rGIBEXIT2\20140827\1210/w27082014,12140198,9112A,77.jpg";
            var result = LprParser.Parse(FakePath, content);
            Assert.Equal(0, result.ConfidenceLevel);
        }

        [Fact]
        public void Parse_InvalidDateFormat_DoesNotThrow()
        {
            const string content = @"NONE\r9112A\r77\rGIBEXIT2\BADDATE\BADTIME/w27082014,12140198,9112A,77.jpg";
            // Should fall back to file modification time — no exception expected
            var ex = Record.Exception(() => LprParser.Parse(FakePath, content));
            Assert.Null(ex);
        }

        // ── Country without leading prefix ────────────────────────────────────

        [Fact]
        public void Parse_CountryWithoutPrefix_PreservedAsIs()
        {
            const string content = @"GBZ\r9112A\r77\rGIBEXIT2\20140827\1210/w27082014,12140198,9112A,77.jpg";
            var result = LprParser.Parse(FakePath, content);
            Assert.Equal("GBZ", result.CountryOfVehicle);
        }
    }
}
