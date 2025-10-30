using BackgroundWorker;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BackgroundWorker
{
    public static class CombinationHelper
    {
        private const int MaxCombinations = 10000; // safety limit

        public static List<CombinationData> GenerateCombinationData(string input, int id)
        {
            var result = new List<CombinationData>();

            if (string.IsNullOrWhiteSpace(input))
                return result;

            var chars = input.ToCharArray();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            GenerateCombinations(chars, 0, new List<char>(), result, seen, id);

            Console.WriteLine($"✅ Generated {result.Count} combinations for AssetId={id}");
            return result;
        }

        private static void GenerateCombinations(
            char[] chars,
            int start,
            List<char> current,
            List<CombinationData> result,
            HashSet<string> seen,
            int id)
        {
            // stop recursion if we exceed safe limit
            if (result.Count >= MaxCombinations)
                return;

            if (current.Count > 0)
            {
                string word = new string(current.ToArray());

                if (seen.Add(word)) // HashSet.Add returns false if already exists
                {
                    int asciiSum = current.Sum(c => (int)c);
                    result.Add(new CombinationData
                    {
                        Word = word,
                        Count = asciiSum,
                        AssetId = id
                    });
                }
            }

            for (int i = start; i < chars.Length; i++)
            {
                current.Add(chars[i]);
                GenerateCombinations(chars, i + 1, current, result, seen, id);
                current.RemoveAt(current.Count - 1);
            }
        }
    }

}
