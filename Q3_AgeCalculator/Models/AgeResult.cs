using System;

namespace Q3_AgeCalculator.Models
{
    public class AgeResult
    {
        public DateTime DateOfBirth { get; set; }
        public DateTime Now { get; set; }

        public int Years { get; set; }
        public int Months { get; set; }
        public int Weeks { get; set; }
        public int Days { get; set; }
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }

        public static AgeResult Calculate(DateTime dob, DateTime now)
        {
            if (dob > now)
                throw new ArgumentException("Date of birth cannot be in the future.");

            int years = now.Year - dob.Year;
            int months = now.Month - dob.Month;
            int days = now.Day - dob.Day;

            if (days < 0)
            {
                months--;
                // Borrow from the previous calendar month; ternary handles the January edge case
                // (previous month is December of the same year, not month 0).
                days += DateTime.DaysInMonth(now.Year, now.Month == 1 ? 12 : now.Month - 1);
            }

            if (months < 0)
            {
                years--;
                months += 12;
            }

            int weeks = days / 7;
            int remainingDays = days % 7;

            int hours = now.Hour - dob.Hour;
            int minutes = now.Minute - dob.Minute;
            int seconds = now.Second - dob.Second;

            if (seconds < 0) { seconds += 60; minutes--; }
            if (minutes < 0) { minutes += 60; hours--; }
            if (hours < 0)   { hours += 24;   remainingDays--; }

            if (remainingDays < 0)
            {
                remainingDays += 7;
                weeks--;
                // weeks += 4 is a ≈28-day approximation; exact borrow is rare and calendar-dependent.
                if (weeks < 0) { weeks += 4; months--; }
            }

            return new AgeResult
            {
                DateOfBirth = dob,
                Now = now,
                Years = years,
                Months = months,
                Weeks = weeks,
                Days = remainingDays,
                Hours = hours,
                Minutes = minutes,
                Seconds = seconds
            };
        }
    }
}
