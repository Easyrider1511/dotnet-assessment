using System;
using Xunit;
using Q2_Quicksort;

namespace Q2_Quicksort.Tests
{
    public class QuickSorterTests
    {
        // ── Sorted output is always ascending ───────────────────────────────

        [Fact]
        public void Sort_AlreadySorted_ReturnsSameOrder()
        {
            double[] input = [1, 2, 3, 4, 5];
            double[] result = QuickSorter.GetSorted(input);
            Assert.Equal([1, 2, 3, 4, 5], result);
        }

        [Fact]
        public void Sort_ReverseSorted_ReturnsSortedAscending()
        {
            double[] input = [5, 4, 3, 2, 1];
            double[] result = QuickSorter.GetSorted(input);
            Assert.Equal([1, 2, 3, 4, 5], result);
        }

        [Fact]
        public void Sort_MixedIntegers_ReturnsSortedAscending()
        {
            double[] input = [3, 1, 4, 1, 5, 9, 2, 6];
            double[] result = QuickSorter.GetSorted(input);
            Assert.Equal([1, 1, 2, 3, 4, 5, 6, 9], result);
        }

        [Fact]
        public void Sort_FloatingPointNumbers_ReturnsSortedAscending()
        {
            double[] input = [3.14, 1.41, 2.71, 0.57];
            double[] result = QuickSorter.GetSorted(input);
            Assert.Equal([0.57, 1.41, 2.71, 3.14], result);
        }

        [Fact]
        public void Sort_MixedIntegersAndFloats_ReturnsSortedAscending()
        {
            double[] input = [5, 2.5, 3, 1.1, 4];
            double[] result = QuickSorter.GetSorted(input);
            Assert.Equal([1.1, 2.5, 3, 4, 5], result);
        }

        // ── Duplicate values (hint requirement) ─────────────────────────────

        [Fact]
        public void Sort_AllDuplicates_ReturnsSameValues()
        {
            double[] input = [7, 7, 7, 7];
            double[] result = QuickSorter.GetSorted(input);
            Assert.Equal([7, 7, 7, 7], result);
        }

        [Fact]
        public void Sort_SomeDuplicates_SortedCorrectly()
        {
            double[] input = [3, 1, 2, 1, 3];
            double[] result = QuickSorter.GetSorted(input);
            Assert.Equal([1, 1, 2, 3, 3], result);
        }

        // ── Edge cases ───────────────────────────────────────────────────────

        [Fact]
        public void Sort_SingleElement_ReturnsSameElement()
        {
            double[] input = [42];
            double[] result = QuickSorter.GetSorted(input);
            Assert.Equal([42], result);
        }

        [Fact]
        public void Sort_TwoElements_ReturnsSortedAscending()
        {
            double[] input = [9, 1];
            double[] result = QuickSorter.GetSorted(input);
            Assert.Equal([1, 9], result);
        }

        [Fact]
        public void Sort_MaximumTenElements_SortedCorrectly()
        {
            double[] input = [10, 9, 8, 7, 6, 5, 4, 3, 2, 1];
            double[] result = QuickSorter.GetSorted(input);
            Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], result);
        }

        [Fact]
        public void Sort_NegativeNumbers_SortedCorrectly()
        {
            double[] input = [-3, -1, -4, -1, -5];
            double[] result = QuickSorter.GetSorted(input);
            Assert.Equal([-5, -4, -3, -1, -1], result);
        }

        [Fact]
        public void Sort_MixedNegativeAndPositive_SortedCorrectly()
        {
            double[] input = [3, -2, 0, 1, -5];
            double[] result = QuickSorter.GetSorted(input);
            Assert.Equal([-5, -2, 0, 1, 3], result);
        }

        // ── GetSorted does not mutate the original array ─────────────────────

        [Fact]
        public void GetSorted_OriginalArrayUnchanged()
        {
            double[] original = [5, 3, 1, 4, 2];
            double[] originalCopy = (double[])original.Clone();
            QuickSorter.GetSorted(original);
            Assert.Equal(originalCopy, original);
        }

        // ── Null guard ────────────────────────────────────────────────────────

        [Fact]
        public void Sort_NullArray_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => QuickSorter.Sort(null!));
        }

        [Fact]
        public void GetSorted_NullArray_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => QuickSorter.GetSorted(null!));
        }
    }
}
