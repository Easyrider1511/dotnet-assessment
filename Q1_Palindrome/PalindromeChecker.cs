using System;
using System.Linq;

namespace Assessment.Q1_Palindrome
{
    public static class PalindromeChecker
    {
        public static bool IsPalindrome(string input)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input), "Input cannot be null.");

            // Strip non-alphanumeric chars so punctuation, whitespace and
            // asymmetrical spaces don't affect the comparison (per the hint).
            ReadOnlySpan<char> normalized = input
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray();

            if (normalized.Length == 0)
                return true;

            // Two-pointer O(n) — avoids allocating a reversed string
            int left = 0, right = normalized.Length - 1;
            while (left < right)
            {
                if (normalized[left] != normalized[right]) return false;
                left++;
                right--;
            }
            return true;
        }

        public static bool IsPalindrome(long number)
        {
            // Negative numbers always have a leading '-' that breaks symmetry
            if (number < 0) return false;
            return IsPalindrome(number.ToString());
        }

        public static bool IsPalindrome(double number)
        {
            if (number < 0) return false;
            return IsPalindrome(number.ToString("G"));
        }
    }
}
