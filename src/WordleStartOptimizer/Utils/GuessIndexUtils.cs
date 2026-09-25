using System.Numerics;

namespace WordleStartOptimizer.Utils;

public static class GuessIndexUtils
{
    public static Dictionary<byte, int> GeneratePatternCounts(this short index, IEnumerable<short> sampleIndexes)
    {
        Dictionary<byte, int> patternCounts = [];
        foreach (short validIndex in sampleIndexes)
        {
            var pattern = Data.PatternMatrix[index, validIndex];

            if (!patternCounts.TryAdd(pattern, 1))
                patternCounts[pattern]++;
        }

        return patternCounts;
    }

    public static Dictionary<byte, int> GeneratePatternCounts<TNumber, TNumber2>(this TNumber index, IEnumerable<TNumber2> sampleIndexes)
        where TNumber : INumber<TNumber>
        where TNumber2 : INumber<TNumber2>
    {
        Dictionary<byte, int> patternCounts = [];
        foreach (short validIndex in sampleIndexes.Select(x => x is short s ? s : short.CreateChecked(x)))
        {
            var pattern = Data.PatternMatrix[index is short s ? s : short.CreateChecked(index), validIndex];

            if (!patternCounts.TryAdd(pattern, 1))
                patternCounts[pattern]++;
        }

        return patternCounts;
    }

    public static TDictionary AddPatternCounts<TDictionary>(this short index, IEnumerable<short> sampleIndexes, TDictionary existingDictionary)
        where TDictionary : IDictionary<byte, int>
    {
        foreach (short validIndex in sampleIndexes)
        {
            var pattern = Data.PatternMatrix[index, validIndex];

            if (!existingDictionary.TryAdd(pattern, 1))
                existingDictionary[pattern]++;
        }

        return existingDictionary;
    }

    public static TDictionary AddPatternCounts<TDictionary, TNumber, TNumber2>(this TNumber index, IEnumerable<TNumber2> sampleIndexes, TDictionary existingDictionary)
        where TDictionary : IDictionary<byte, int>
        where TNumber : INumber<TNumber>
        where TNumber2 : INumber<TNumber2>
    {
        foreach (short validIndex in sampleIndexes.Select(x => x is short s ? s : short.CreateChecked(x)))
        {
            var pattern = Data.PatternMatrix[index is short s ? s : short.CreateChecked(index), validIndex];

            if (!existingDictionary.TryAdd(pattern, 1))
                existingDictionary[pattern]++;
        }

        return existingDictionary;
    }
}