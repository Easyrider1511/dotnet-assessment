using System;
using Xunit;
using Q3_AgeCalculator.Models;

namespace Q3_AgeCalculator.Tests
{
    public class AgeResultTests
    {
        // ── Spec example from the assessment ────────────────────────────────

        [Fact]
        public void Calculate_SpecExample_ReturnsCorrectAge()
        {
            // Input: 01/01/2000  |  Today: 20/03/2018  →  18 years, 2 months
            var dob = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var now = new DateTime(2018, 3, 20, 0, 0, 0, DateTimeKind.Unspecified);

            var result = AgeResult.Calculate(dob, now);

            Assert.Equal(18, result.Years);
            Assert.Equal(2, result.Months);
        }

        // ── Years ────────────────────────────────────────────────────────────

        [Fact]
        public void Calculate_ExactBirthday_YearsIncrements()
        {
            var dob = new DateTime(1990, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
            var now = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);

            var result = AgeResult.Calculate(dob, now);

            Assert.Equal(35, result.Years);
            Assert.Equal(0, result.Months);
            Assert.Equal(0, result.Weeks);
            Assert.Equal(0, result.Days);
        }

        [Fact]
        public void Calculate_DayBeforeBirthday_YearsNotYetIncremented()
        {
            var dob = new DateTime(1990, 6, 15, 0, 0, 0, DateTimeKind.Unspecified);
            var now = new DateTime(2025, 6, 14, 0, 0, 0, DateTimeKind.Unspecified);

            var result = AgeResult.Calculate(dob, now);

            Assert.Equal(34, result.Years);
        }

        // ── Months ───────────────────────────────────────────────────────────

        [Fact]
        public void Calculate_SameYearDifferentMonth_CorrectMonths()
        {
            var dob = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var now = new DateTime(2000, 4, 1, 0, 0, 0, DateTimeKind.Unspecified);

            var result = AgeResult.Calculate(dob, now);

            Assert.Equal(0, result.Years);
            Assert.Equal(3, result.Months);
        }

        [Fact]
        public void Calculate_MonthRollover_BorrowsFromYear()
        {
            // Born March, measured in January the following year → 10 months
            var dob = new DateTime(2000, 3, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var now = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

            var result = AgeResult.Calculate(dob, now);

            Assert.Equal(0, result.Years);
            Assert.Equal(10, result.Months);
        }

        // ── Days / Weeks ──────────────────────────────────────────────────────

        [Fact]
        public void Calculate_ExactlyFourteenDays_TwoWeeksZeroDays()
        {
            var dob = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var now = new DateTime(2000, 1, 15, 0, 0, 0, DateTimeKind.Unspecified);

            var result = AgeResult.Calculate(dob, now);

            Assert.Equal(0, result.Years);
            Assert.Equal(0, result.Months);
            Assert.Equal(2, result.Weeks);
            Assert.Equal(0, result.Days);
        }

        [Fact]
        public void Calculate_TenDays_OneWeekThreeDays()
        {
            var dob = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var now = new DateTime(2000, 1, 11, 0, 0, 0, DateTimeKind.Unspecified);

            var result = AgeResult.Calculate(dob, now);

            Assert.Equal(1, result.Weeks);
            Assert.Equal(3, result.Days);
        }

        // ── Hours / Minutes / Seconds ─────────────────────────────────────────

        [Fact]
        public void Calculate_TimeComponent_CorrectHoursMinutesSeconds()
        {
            var dob = new DateTime(2000, 1, 1, 8, 30, 45, DateTimeKind.Unspecified);
            var now = new DateTime(2000, 1, 1, 10, 45, 50, DateTimeKind.Unspecified);

            var result = AgeResult.Calculate(dob, now);

            Assert.Equal(2, result.Hours);
            Assert.Equal(15, result.Minutes);
            Assert.Equal(5, result.Seconds);
        }

        [Fact]
        public void Calculate_SecondsCarry_BorrowsFromMinutes()
        {
            var dob = new DateTime(2000, 1, 1, 0, 0, 50, DateTimeKind.Unspecified);
            var now = new DateTime(2000, 1, 1, 0, 1, 10, DateTimeKind.Unspecified);

            var result = AgeResult.Calculate(dob, now);

            Assert.Equal(0, result.Hours);
            Assert.Equal(0, result.Minutes);
            Assert.Equal(20, result.Seconds);
        }

        [Fact]
        public void Calculate_MinutesCarry_BorrowsFromHours()
        {
            var dob = new DateTime(2000, 1, 1, 1, 50, 0, DateTimeKind.Unspecified);
            var now = new DateTime(2000, 1, 1, 2, 10, 0, DateTimeKind.Unspecified);

            var result = AgeResult.Calculate(dob, now);

            Assert.Equal(0, result.Hours);
            Assert.Equal(20, result.Minutes);
        }

        // ── Edge cases ────────────────────────────────────────────────────────

        [Fact]
        public void Calculate_SameMoment_AllZero()
        {
            var moment = new DateTime(2000, 6, 15, 12, 30, 0, DateTimeKind.Unspecified);
            var result = AgeResult.Calculate(moment, moment);

            Assert.Equal(0, result.Years);
            Assert.Equal(0, result.Months);
            Assert.Equal(0, result.Weeks);
            Assert.Equal(0, result.Days);
            Assert.Equal(0, result.Hours);
            Assert.Equal(0, result.Minutes);
            Assert.Equal(0, result.Seconds);
        }

        [Fact]
        public void Calculate_FutureDateOfBirth_ThrowsArgumentException()
        {
            var dob = DateTime.Now.AddDays(1);
            Assert.Throws<ArgumentException>(() => AgeResult.Calculate(dob, DateTime.Now));
        }

        [Fact]
        public void Calculate_BornOnLeapDay_HandledWithoutException()
        {
            var dob = new DateTime(2000, 2, 29, 0, 0, 0, DateTimeKind.Unspecified);
            var now = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Unspecified);

            var ex = Record.Exception(() => AgeResult.Calculate(dob, now));
            Assert.Null(ex);
        }

        [Fact]
        public void Calculate_DateOfBirthAndNowPreserved()
        {
            var dob = new DateTime(1985, 5, 20, 0, 0, 0, DateTimeKind.Unspecified);
            var now = new DateTime(2025, 5, 20, 0, 0, 0, DateTimeKind.Unspecified);

            var result = AgeResult.Calculate(dob, now);

            Assert.Equal(dob, result.DateOfBirth);
            Assert.Equal(now, result.Now);
        }

        // ── Day-borrow when now is in January ─────────────────────────────────

        [Fact]
        public void Calculate_DayBorrowInJanuary_UsesPreviousDecember()
        {
            // now.Month == 1 hits the ternary's true branch: borrows from December (31 days)
            // dob=Dec 15 1999, now=Jan 5 2000 → 21 raw days = 3 weeks exactly
            var dob = new DateTime(1999, 12, 15, 0, 0, 0, DateTimeKind.Unspecified);
            var now = new DateTime(2000, 1, 5, 0, 0, 0, DateTimeKind.Unspecified);

            var result = AgeResult.Calculate(dob, now);

            Assert.Equal(0, result.Years);
            Assert.Equal(0, result.Months);
            Assert.Equal(3, result.Weeks);
            Assert.Equal(0, result.Days);
        }

        // ── Hours borrow chain ────────────────────────────────────────────────

        [Fact]
        public void Calculate_HoursCarry_BorrowsFromRemainingDays()
        {
            // hours < 0 → remainingDays-- ; remainingDays < 0 → borrow 7 days ; weeks-- (stays ≥ 0)
            // dob=Jan 8 23:00, now=Jan 15 22:00 → 7 raw days (1 week, 0 rem), then hours borrow
            var dob = new DateTime(2000, 1, 8, 23, 0, 0, DateTimeKind.Unspecified);
            var now = new DateTime(2000, 1, 15, 22, 0, 0, DateTimeKind.Unspecified);

            var result = AgeResult.Calculate(dob, now);

            Assert.Equal(0, result.Weeks);
            Assert.Equal(6, result.Days);
            Assert.Equal(23, result.Hours);
        }

        [Fact]
        public void Calculate_HoursCarryChain_WeeksNegativeBorrowsFromMonths()
        {
            // hours < 0 → remainingDays-- → weeks-- goes negative → borrows 4 weeks from months
            // dob=Jan 1 23:00, now=Feb 1 22:00 → 0 raw days (weeks=0), then hours borrow cascades
            var dob = new DateTime(2000, 1, 1, 23, 0, 0, DateTimeKind.Unspecified);
            var now = new DateTime(2000, 2, 1, 22, 0, 0, DateTimeKind.Unspecified);

            var result = AgeResult.Calculate(dob, now);

            Assert.Equal(0, result.Years);
            Assert.Equal(0, result.Months);
            Assert.Equal(3, result.Weeks);
            Assert.Equal(6, result.Days);
            Assert.Equal(23, result.Hours);
        }
    }
}
