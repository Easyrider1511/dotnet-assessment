using System;
using Q2_Quicksort;

const int MaxElements = 10;

Console.WriteLine("=== Quicksort ===");
Console.Write($"Number of elements to sort (1-{MaxElements}): ");

if (!int.TryParse(Console.ReadLine(), out int count) || count < 1 || count > MaxElements)
{
    Console.WriteLine($"Please enter a number between 1 and {MaxElements}.");
    return;
}

double[] elements = new double[count];

for (int i = 0; i < count; i++)
{
    Console.Write($"Element {i + 1}: ");
    while (true)
    {
        var raw = Console.ReadLine();
        if (raw is null) { Console.WriteLine("Unexpected end of input."); return; }
        if (double.TryParse(raw, out elements[i])) break;
        Console.Write($"Invalid. Element {i + 1}: ");
    }
}

double[] sorted = QuickSorter.GetSorted(elements);

Console.WriteLine("\nOriginal order: " + string.Join(", ", elements));
Console.WriteLine("Sorted order:   " + string.Join(", ", sorted));
