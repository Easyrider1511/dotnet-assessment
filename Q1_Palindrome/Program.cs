using System;
using Assessment.Q1_Palindrome;

Console.WriteLine("=== Palindrome Checker ===");
Console.WriteLine("Enter a word, phrase, or number (or 'quit' to exit).");
Console.WriteLine();

while (true)
{
    Console.Write("Input: ");
    var input = Console.ReadLine();

    if (input is null || input.Equals("quit", StringComparison.OrdinalIgnoreCase))
        break;

    bool result = PalindromeChecker.IsPalindrome(input);
    Console.WriteLine(result
        ? $"  \"{input}\" IS a palindrome."
        : $"  \"{input}\" is NOT a palindrome.");
    Console.WriteLine();
}
