using System.Numerics;
using WordleStartOptimizer.Models;

namespace WordleStartOptimizer.Utils;

public static class GuessIndexUtils
{
    public static PatternCounter<byte> GeneratePatternCounts(this short index, IEnumerable<short> sampleIndexes)
    {
        var indexArr = sampleIndexes as short[] ?? sampleIndexes.ToArray();
        var patternCounts = new PatternCounter<byte>(indexArr.Length);

        foreach (short validIndex in indexArr)
            patternCounts.Add(Data.PatternMatrix[index, validIndex]);

        return patternCounts;
    }

    public static PatternCounter<byte> GeneratePatternCounts<TNumber, TNumber2>(this TNumber index, IEnumerable<TNumber2> sampleIndexes)
        where TNumber : INumber<TNumber>
        where TNumber2 : INumber<TNumber2>
    {
        var indexArr = sampleIndexes as TNumber2[] ?? sampleIndexes.ToArray();
        var patternCounts = new PatternCounter<byte>(indexArr.Length);

        foreach (var validIndex in indexArr)
            patternCounts.Add(Data.PatternMatrix[index is short s ? s : short.CreateChecked(index), validIndex is short s2 ? s2 : short.CreateChecked(validIndex)]);

        return patternCounts;
    }

    public static PatternCounter<byte> AddPatternCounts(this short index, IEnumerable<short> sampleIndexes, PatternCounter<byte> existingCounter)
    {
        foreach (short validIndex in sampleIndexes)
            existingCounter.Add(Data.PatternMatrix[index, validIndex]);

        return existingCounter;
    }

    public static PatternCounter<byte> AddPatternCounts<TNumber, TNumber2>(this TNumber index, IEnumerable<TNumber2> sampleIndexes, PatternCounter<byte> existingCounter)
        where TNumber : INumber<TNumber>
        where TNumber2 : INumber<TNumber2>
    {
        foreach (short validIndex in sampleIndexes.Select(x => x is short s ? s : short.CreateChecked(x)))
            existingCounter.Add(Data.PatternMatrix[short.CreateChecked(index), validIndex]);

        return existingCounter;
    }
}