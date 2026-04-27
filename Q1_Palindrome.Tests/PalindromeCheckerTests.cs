using System;
using Xunit;
using Assessment.Q1_Palindrome;

namespace Assessment.Q1_Palindrome.Tests
{
    /// <summary>
    /// Comprehensive unit tests for PalindromeChecker.
    /// Covers: basic words, mixed case, numbers, special characters,
    /// punctuation, asymmetric whitespace, edge cases, and null input.
    /// </summary>
    public class PalindromeCheckerTests
    {
        // ──────────────────────────────────────────────
        // TRUE palindromes
        // ──────────────────────────────────────────────

        [Theory]
        [InlineData("Deleveled")]         // Spec example – mixed case
        [InlineData("deleveled")]         // Lowercase
        [InlineData("DELEVELED")]         // Uppercase
        [InlineData("racecar")]           // Classic
        [InlineData("Racecar")]           // Mixed case
        [InlineData("madam")]
        [InlineData("level")]
        [InlineData("noon")]
        [InlineData("civic")]
        [InlineData("radar")]
        [InlineData("refer")]
        [InlineData("rotator")]
        [InlineData("repaper")]
        [InlineData("a")]                 // Single character
        [InlineData("A")]
        [InlineData("aa")]
        [InlineData("Aba")]
        public void IsPalindrome_SingleWordTrueCases_ReturnsTrue(string input)
        {
            Assert.True(PalindromeChecker.IsPalindrome(input));
        }

        [Theory]
        [InlineData("A man a plan a canal Panama")]  // Classic phrase with spaces
        [InlineData("Was it a car or a cat I saw?")] // Punctuation + spaces
        [InlineData("No 'x' in Nixon")]              // Apostrophes + spaces
        [InlineData("Never odd or even")]
        [InlineData("Do geese see God?")]
        [InlineData("Step on no pets")]
        [InlineData("Able was I ere I saw Elba")]
        [InlineData("Eva, can I see bees in a cave?")]
        [InlineData("Madam, I'm Adam")]
        [InlineData("  racecar  ")]                  // Asymmetric whitespace (hint)
        [InlineData("  r a c e c a r  ")]            // Embedded spaces
        [InlineData("race   car")]                   // Asymmetric internal whitespace
        public void IsPalindrome_PhrasesAndSpecialCharacters_ReturnsTrue(string input)
        {
            Assert.True(PalindromeChecker.IsPalindrome(input));
        }

        [Theory]
        [InlineData("12321")]
        [InlineData("1221")]
        [InlineData("1")]
        [InlineData("11")]
        [InlineData("1001")]
        public void IsPalindrome_NumericStrings_ReturnsTrue(string input)
        {
            Assert.True(PalindromeChecker.IsPalindrome(input));
        }

        [Theory]
        [InlineData(12321L)]
        [InlineData(1L)]
        [InlineData(11L)]
        [InlineData(121L)]
        [InlineData(1001L)]
        [InlineData(0L)]
        public void IsPalindrome_LongOverload_TrueCases_ReturnsTrue(long number)
        {
            Assert.True(PalindromeChecker.IsPalindrome(number));
        }

        // ──────────────────────────────────────────────
        // FALSE palindromes
        // ──────────────────────────────────────────────

        [Theory]
        [InlineData("hello")]
        [InlineData("world")]
        [InlineData("dotnet")]
        [InlineData("palindrome")]
        [InlineData("assessment")]
        [InlineData("OpenAI")]
        [InlineData("12345")]
        [InlineData("ab")]
        [InlineData("abc")]
        [InlineData("abca")]
        public void IsPalindrome_NonPalindromes_ReturnsFalse(string input)
        {
            Assert.False(PalindromeChecker.IsPalindrome(input));
        }

        [Theory]
        [InlineData(-121L)]   // Negative numbers are never palindromes
        [InlineData(-1L)]
        [InlineData(123L)]
        [InlineData(1234L)]
        public void IsPalindrome_LongOverload_FalseCases_ReturnsFalse(long number)
        {
            Assert.False(PalindromeChecker.IsPalindrome(number));
        }

        // ──────────────────────────────────────────────
        // Edge cases
        // ──────────────────────────────────────────────

        [Fact]
        public void IsPalindrome_EmptyString_ReturnsTrue()
        {
            Assert.True(PalindromeChecker.IsPalindrome(string.Empty));
        }

        [Fact]
        public void IsPalindrome_WhitespaceOnly_ReturnsTrue()
        {
            // After stripping non-alphanumeric characters, the string is empty → palindrome
            Assert.True(PalindromeChecker.IsPalindrome("   "));
            Assert.True(PalindromeChecker.IsPalindrome("\t\n"));
        }

        [Fact]
        public void IsPalindrome_PunctuationOnly_ReturnsTrue()
        {
            Assert.True(PalindromeChecker.IsPalindrome("!@#$%"));
            Assert.True(PalindromeChecker.IsPalindrome("..."));
            Assert.True(PalindromeChecker.IsPalindrome("?!?"));
        }

        [Fact]
        public void IsPalindrome_NullInput_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                PalindromeChecker.IsPalindrome((string)null!));

            Assert.Equal("input", ex.ParamName);
        }

        [Fact]
        public void IsPalindrome_SpecialCharactersEmbedded_IgnoredCorrectly()
        {
            // "race#$car" normalises to "racecar" → palindrome
            Assert.True(PalindromeChecker.IsPalindrome("race#$car"));

            // "hello#$world" normalises to "helloworld" → not a palindrome
            Assert.False(PalindromeChecker.IsPalindrome("hello#$world"));
        }

        [Fact]
        public void IsPalindrome_AsymmetricWhitespace_IgnoredPerHint()
        {
            // Hint: watch out for asymmetrical whitespaces
            Assert.True(PalindromeChecker.IsPalindrome("  level    "));
            Assert.False(PalindromeChecker.IsPalindrome("  hello    "));
        }

        [Fact]
        public void IsPalindrome_SingleCharacter_ReturnsTrue()
        {
            Assert.True(PalindromeChecker.IsPalindrome("z"));
            Assert.True(PalindromeChecker.IsPalindrome("Z"));
            Assert.True(PalindromeChecker.IsPalindrome("5"));
        }

        // ──────────────────────────────────────────────
        // Double overload
        // ──────────────────────────────────────────────

        [Theory]
        [InlineData(0.0)]        // Single digit
        [InlineData(11.0)]       // Two equal digits
        [InlineData(12321.0)]    // Classic numeric palindrome
        public void IsPalindrome_DoubleOverload_TrueCases_ReturnsTrue(double number)
        {
            Assert.True(PalindromeChecker.IsPalindrome(number));
        }

        [Theory]
        [InlineData(-1.0)]       // Negative numbers are never palindromes
        [InlineData(-121.0)]
        [InlineData(123.0)]      // Non-palindrome positive
        public void IsPalindrome_DoubleOverload_FalseCases_ReturnsFalse(double number)
        {
            Assert.False(PalindromeChecker.IsPalindrome(number));
        }
    }
}
