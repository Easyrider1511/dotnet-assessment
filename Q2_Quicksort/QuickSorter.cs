using System;

namespace Q2_Quicksort
{
    public static class QuickSorter
    {
        public static void Sort(double[] arr)
        {
            if (arr is null) throw new ArgumentNullException(nameof(arr));
            Sort(arr, 0, arr.Length - 1);
        }

        private static void Sort(double[] arr, int low, int high)
        {
            if (low >= high) return;

            int pivotIndex = Partition(arr, low, high);
            Sort(arr, low, pivotIndex - 1);
            Sort(arr, pivotIndex + 1, high);
        }

        private static int Partition(double[] arr, int low, int high)
        {
            // Last-element pivot: simple and correct, but degrades to O(n²) on already-sorted input.
            double pivot = arr[high];
            int i = low - 1;

            for (int j = low; j < high; j++)
            {
                if (arr[j] <= pivot)
                {
                    i++;
                    (arr[i], arr[j]) = (arr[j], arr[i]);
                }
            }

            (arr[i + 1], arr[high]) = (arr[high], arr[i + 1]);
            return i + 1;
        }

        public static double[] GetSorted(double[] arr)
        {
            if (arr is null) throw new ArgumentNullException(nameof(arr));
            double[] copy = (double[])arr.Clone();
            Sort(copy);
            return copy;
        }
    }
}
