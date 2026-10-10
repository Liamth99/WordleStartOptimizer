using System.Diagnostics;

namespace WordleStartOptimizer.Models;

[DebuggerDisplay("{DisplayString}")]
public sealed class WordSet
{
    private string DisplayString => string.Join(", ", Words);

    public short[] WordIndexes { get; init; }
    public IEnumerable<string> Words => WordIndexes.Select(x => Data.ValidGuesses[x]);

    public int Count { get; init; }

    [ThreadStatic]
    private static PatternCounter<ulong>? _patternCountCache;

    [ThreadStatic]
    private static ulong[]? _codes;

    private const uint FirstYVowelMask  = 25U << 5;
    private const uint SecondYVowelMask = 25U << 10;
    private const uint ThirdYVowelMask  = 25U << 15;

    public WordSet(short[] wordIndexes)
    {
        Count       = wordIndexes.Length;
        WordIndexes = new short[Count];

        if (_patternCountCache is null)
            _patternCountCache = new (Data.ValidGuesses.Length);
        else
            _patternCountCache.Clear();
        _codes ??= new ulong[Data.ValidGuesses.Length];

        int  letterMask    = 0;
        bool yVowelPresent = false;
        AvgGreen     = 0D;
        AvgYellow    = 0D;
        ValidAnswers = 0;

        for (int answerIndex = 0; answerIndex < Count; answerIndex++)
        {
            WordIndexes[answerIndex] = wordIndexes[answerIndex];
            var index = WordIndexes[answerIndex];

            letterMask |= Math.Abs(Data.ProcessedGuesses[index].LetterMask);
            AvgGreen  += Data.GreenLetters[index];
            AvgYellow += Data.YellowLetters[index];

            if (!yVowelPresent)
            {
                var mask = Data.ProcessedGuesses[index].Mask;
                yVowelPresent =
                    (mask & FirstYVowelMask) is FirstYVowelMask ||
                    (mask & SecondYVowelMask) is SecondYVowelMask ||
                    (mask & ThirdYVowelMask) is ThirdYVowelMask;
            }

            if (Data.WordIsValidAnswer[index])
                ValidAnswers++;

            var rowSpan = Data.GetPatternRow(index);

            if (answerIndex is 0)
                for (int i = 0; i < Data.ValidGuesses.Length; i++)
                    _codes[i] = rowSpan[i];
            else
                for (int i = 0; i < Data.ValidGuesses.Length; i++)
                    _codes[i] = _codes[i] * 243 + rowSpan[i];
        }

        for (int i = 0; i < Data.ValidGuesses.Length; i++)
            _patternCountCache.Add(_codes[i]);

        var stats = _patternCountCache.CalculateStatistics(Data.ProcessedGuesses.Length);

        Entropy            = stats.entropy;
        WorstCaseRemaining = stats.worstCase;
        ExpectedRemaining  = stats.expectedRemaining;

        VowelScore = 0D;

        if((letterMask & 1) is not 0)
            VowelScore += Data.LetterDistribution['a'];
        if((letterMask & (1U << 4)) is not 0)
            VowelScore += Data.LetterDistribution['e'];
        if((letterMask & (1U << 8)) is not 0)
            VowelScore += Data.LetterDistribution['i'];
        if((letterMask & (1U << 14)) is not 0)
            VowelScore += Data.LetterDistribution['o'];
        if((letterMask & (1U << 20)) is not 0)
            VowelScore += Data.LetterDistribution['u'];
        if(yVowelPresent)
            VowelScore += Data.YAsVowelDistributionScore;

        VowelScore /= Data.TotalVowelLetterDistributionScore;
    }

    public PatternCounter<ulong> GetPatternCounts()
    {
        var counter = new PatternCounter<ulong>(Data.ValidGuesses.Length);

        for (short answerIndex = 0; answerIndex < Data.ProcessedGuesses.Length; answerIndex++)
        {
            ulong combinedPatternCode = 0;
            ulong multiplier          = 1;

            foreach (short guessIndex in WordIndexes)
            {
                combinedPatternCode += Data.PatternMatrix[guessIndex, answerIndex] * multiplier;
                multiplier          *= 243;
            }

            counter.Add(combinedPatternCode);
        }

        return counter;
    }

    public WordSet SubSet(int index)
    {
        if (index > Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Must be less than or equal to the word set count.");

        return new WordSet(WordIndexes.Take(index + 1).ToArray());
    }

    public WordSet SubSet(int startIndex, int count)
    {
        if (startIndex > Count)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "Must be less than or equal to the word set count.");

        return new WordSet(WordIndexes.Skip(startIndex).Take(count).ToArray());
    }

    public double VowelScore { get; }
    public double Entropy { get; }
    public double AvgGreen { get; }
    public double AvgYellow { get; }
    public double ExpectedRemaining { get; }
    public double WorstCaseRemaining { get; }
    public int ValidAnswers { get; }
}