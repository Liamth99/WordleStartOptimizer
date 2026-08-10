using System.Diagnostics;

namespace WordleStartOptimizer.Models;

[DebuggerDisplay("{Score} - {DisplayString}")]
public sealed class WordSet
{
    private string DisplayString => string.Join(", ", Words);

    public string[] Words { get; set; }

    public double Score => Program.Options.EntropyModifier                 * NormalizedEntropy +
                           Program.Options.ExpectedRemainingModifier       * NormalizedExpectedRemaining +
                           Program.Options.WorstCaseRemainingModifier      * NormalizedWorstCaseRemaining +
                           Program.Options.GreenLetterModifier             * NormalizedGreen +
                           Program.Options.YellowLetterModifier            * NormalizedYellow +
                           Program.Options.VowelCountModifier              * NormalizedVowelCount +
                           Program.Options.LetterDistributionOrderModifier * LetterDistributionOrder;


    [ThreadStatic]
    private static Dictionary<long, int>? _patternCountCache;
    private static Lock _lock = new();

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

        Green        = 0;
        Yellow       = 0;
        ValidAnswers = 0;

        for (int i = 0; i < wordIndexes.Length; i++)
        {
            var index = wordIndexes[i];

            Green  += Data.GreenLetters[index];
            Yellow += Data.YellowLetters[index];

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

        lock (_lock)
        {
            if (Green < MinGreen)
                MinGreen = Green;

            if (Green > MaxGreen)
                MaxGreen = Green;

            if (Entropy < MinEntropy)
                MinEntropy = Entropy;

            if (Entropy > MaxEntropy)
                MaxEntropy = Entropy;

            if (Yellow < MinYellow)
                MinYellow = Yellow;

            if (Yellow > MaxYellow)
                MaxYellow = Yellow;

            if (ExpectedRemaining < MinExpectedRemaining)
                MinExpectedRemaining = ExpectedRemaining;

            if (ExpectedRemaining > MaxExpectedRemaining)
                MaxExpectedRemaining = ExpectedRemaining;

            if (WorstCaseRemaining < MinWorstCaseRemaining)
                MinWorstCaseRemaining = WorstCaseRemaining;

            if (WorstCaseRemaining > MaxWorstCaseRemaining)
                MaxWorstCaseRemaining = WorstCaseRemaining;

            if (LetterDistributionOrder < MinLetterDistributionOrder)
                MinLetterDistributionOrder = LetterDistributionOrder;

            if (LetterDistributionOrder > MaxLetterDistributionOrder)
                MaxLetterDistributionOrder = LetterDistributionOrder;
        }
    }

    public int VowelCount => Words.Sum(x => x.Count(c => c is 'a' or 'e' or 'i' or 'o' or 'u' or 'y'));
    public double NormalizedVowelCount => VowelCount / 6D;

    public        double Entropy       { get; private set; }
    public        double NormalizedEntropy => (Entropy - MinEntropy) / (MaxEntropy - MinEntropy);
    public static double MinEntropy    { get; private set; } = double.MaxValue;
    public static double MaxEntropy    { get; private set; }

    public        int    Green       { get; private set; }
    public        double NormalizedGreen => (Green - MinGreen) / (double)(MaxGreen - MinGreen);
    public static int    MinGreen    { get; private set; } = int.MaxValue;
    public static int    MaxGreen    { get; private set; }


    public        int    Yellow       { get; private set; }
    public        double NormalizedYellow => (Yellow - MinYellow) / (double)(MaxYellow - MinYellow);
    public static int    MinYellow    { get; private set; } = int.MaxValue;
    public static int    MaxYellow    { get; private set; }


    public readonly double ExpectedRemaining;
    public          double NormalizedExpectedRemaining => 1 - (ExpectedRemaining - MinExpectedRemaining) / (MaxExpectedRemaining - MinExpectedRemaining);
    public static   double MinExpectedRemaining    { get; private set; } = int.MaxValue;
    public static   double MaxExpectedRemaining    { get; private set; }


    public readonly double WorstCaseRemaining;
    public          double NormalizedWorstCaseRemaining => 1 - (WorstCaseRemaining - MinWorstCaseRemaining) / (MaxWorstCaseRemaining - MinWorstCaseRemaining);
    public static   double MinWorstCaseRemaining    { get; private set; } = int.MaxValue;
    public static   double MaxWorstCaseRemaining    { get; private set; }


    public readonly double LetterDistributionOrder;
    public          double NormalizedLetterDistributionOrder => (LetterDistributionOrder - MinLetterDistributionOrder) / (MaxLetterDistributionOrder - MinLetterDistributionOrder);
    public static   double MinLetterDistributionOrder    { get; private set; } = int.MaxValue;
    public static   double MaxLetterDistributionOrder    { get; private set; }

    public readonly int ValidAnswers;
}