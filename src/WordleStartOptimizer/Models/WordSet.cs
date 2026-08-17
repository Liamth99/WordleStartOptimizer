using System.Diagnostics;

namespace WordleStartOptimizer.Models;

[DebuggerDisplay("{Score} - {DisplayString}")]
public sealed class WordSet
{
    private string DisplayString => string.Join(", ", Words);

    public string[] Words { get; set; }


    [ThreadStatic]
    private static Dictionary<long, int>? _patternCountCache;

    public WordSet(short[] wordIndexes)
    {
        if (_patternCountCache is null)
            _patternCountCache = new Dictionary<long, int>(Data.ValidGuesses.Length);
        else
            _patternCountCache.Clear();

        for (int answerIndex = 0; answerIndex < Data.ProcessedGuesses.Length; answerIndex++)
        {
            long combinedPatternCode = 0;
            long multiplier          = 1;

            foreach (int guessIndex in wordIndexes)
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

        for (int i = 0; i < wordIndexes.Length; i++)
        {
            var index = wordIndexes[i];

            AvgGreen  += Data.GreenLetters[index];
            AvgYellow += Data.YellowLetters[index];

            if (Data.WordIsValidAnswer[index])
                ValidAnswers++;
        }

        Words = wordIndexes.Zip(wordIndexes.Select(x => Data.ValidGuesses[x]))
                           .OrderByDescending(x => Data.WordLetterDistributionScore[x.First])
                           .Select(x => x.Second)
                           .ToArray();

        LetterDistributionOrder = wordIndexes
                                 .Select(x => Data.WordLetterDistributionScore[x])
                                 .Order()
                                 .Select((x, i) => x * (1 + .2 * i))
                                 .Sum();

        VowelCount = Words.Sum(x => x.Count(c => c is 'a' or 'e' or 'i' or 'o' or 'u' or 'y'));

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

    public int VowelCount { get; }
    public double Entropy { get; }
    public double AvgGreen { get; }
    public double AvgYellow { get; }
    public double ExpectedRemaining { get; }
    public double WorstCaseRemaining { get; }
    public double LetterDistributionOrder { get; }
    public int ValidAnswers { get; }
}