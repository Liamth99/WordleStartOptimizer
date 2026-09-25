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
    private static Dictionary<long, int>? _patternCountCache;

    public WordSet(short[] wordIndexes)
    {
        if (_patternCountCache is null)
            _patternCountCache = new Dictionary<long, int>(Data.ValidGuesses.Length);
        else
            _patternCountCache.Clear();

        for (short answerIndex = 0; answerIndex < Data.ProcessedGuesses.Length; answerIndex++)
        {
            long combinedPatternCode = 0;
            long multiplier          = 1;

            foreach (short guessIndex in wordIndexes)
            {
                combinedPatternCode += Data.PatternMatrix[guessIndex, answerIndex] * multiplier;
                multiplier          *= 243;
            }

            if (!_patternCountCache.TryAdd(combinedPatternCode, 1))
                _patternCountCache[combinedPatternCode]++;
        }

        AvgGreen     = 0;
        AvgYellow    = 0;
        ValidAnswers = 0;

        for (short i = 0; i < wordIndexes.Length; i++)
        {
            var index = wordIndexes[i];

            AvgGreen  += Data.GreenLetters[index];
            AvgYellow += Data.YellowLetters[index];

            if (Data.WordIsValidAnswer[index])
                ValidAnswers++;
        }

        WordIndexes = wordIndexes.OrderByDescending(x => Data.WordLetterDistributionScore[x]).ToArray();
        var wordArr = Words.ToArray();
        Count       = wordIndexes.Length;

        VowelScore = 0D;

        var aPresent = false;
        var ePresent = false;
        var iPresent = false;
        var oPresent = false;
        var uPresent = false;
        var yPresent = false;

        for (int i = 0; i < Count; i++)
        for (int j = 0; j < 5; j++)
        {
            var c = wordArr[i][j];

            switch (c)
            {
                case 'a':
                    aPresent = true;
                    break;
                case 'e':
                    ePresent = true;
                    break;
                case 'i':
                    iPresent = true;
                    break;
                case 'o':
                    oPresent = true;
                    break;
                case 'u':
                    uPresent = true;
                    break;
                case 'y':
                    if(j is not 0 and not 4)
                        yPresent = true;
                    break;
            }
        }

        if(aPresent)
            VowelScore += Data.LetterDistribution['a'];
        if(ePresent)
            VowelScore += Data.LetterDistribution['e'];
        if(iPresent)
            VowelScore += Data.LetterDistribution['i'];
        if(oPresent)
            VowelScore += Data.LetterDistribution['o'];
        if(uPresent)
            VowelScore += Data.LetterDistribution['u'];
        if(yPresent)
            VowelScore += Data.YAsVowelDistributionScore;

        VowelScore /= Data.TotalVowelLetterDistributionScore;

        Entropy            = 0D;
        WorstCaseRemaining = 0;

        double total                = Data.ProcessedGuesses.Length;
        var    expectedRemainingSum = 0;

        foreach (int count in _patternCountCache.Values)
        {
            Entropy += Data.EntropyContributionByCount[count];

            expectedRemainingSum += count * count;

            if (count > WorstCaseRemaining)
                WorstCaseRemaining = count;
        }

        ExpectedRemaining = expectedRemainingSum / total;
    }

    public Dictionary<long, int> GetPatternCounts()
    {
        var dic = new Dictionary<long, int>(Data.ValidGuesses.Length);

        for (short answerIndex = 0; answerIndex < Data.ProcessedGuesses.Length; answerIndex++)
        {
            long combinedPatternCode = 0;
            long multiplier          = 1;

            foreach (short guessIndex in WordIndexes)
            {
                combinedPatternCode += Data.PatternMatrix[guessIndex, answerIndex] * multiplier;
                multiplier          *= 243;
            }

            if (!dic.TryAdd(combinedPatternCode, 1))
                dic[combinedPatternCode]++;
        }

        return dic;
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