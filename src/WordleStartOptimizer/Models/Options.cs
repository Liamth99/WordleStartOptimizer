using System.Diagnostics.CodeAnalysis;
using CommandLine;
using CommandLine.Text;

namespace WordleStartOptimizer.Models;

[SuppressMessage("Design", "LOCAT001:Missing Debug Display, should include data from members")]
public class Options
{
    [Value(0, MetaName = "Set Size", HelpText = "The number of starting words to generate.", Required = true)]
    public int SetSize
    {
        get;
        init
        {
            if(value < 0)
                throw new ArgumentOutOfRangeException(nameof(SetSize), "Set size must be larger than 0");

            if(value >= 6)
                    throw new ArgumentOutOfRangeException(nameof(SetSize), "Set size must be less than 6");

            field = value;
        }
    }

    [Option("effort", Default = EffortLevel.Normal, HelpText = "How many candidates to fully score after pre-scoring. Values: Low, Normal, High, Max.")]
    public EffortLevel Effort { get; init; }

    public double EffortPercentage => Effort switch
    {
        EffortLevel.Low    => 0.05,
        EffortLevel.Normal => 0.25,
        EffortLevel.High   => 0.50,
        EffortLevel.Max    => 1.00,
        _                  => throw new ArgumentOutOfRangeException(),
    };

    [Option('e', "entropy", Default = 1.0, HelpText = "Weight for maximizing information gained from each guess. Higher values favor guesses that split the solution space more evenly.")]
    public double EntropyModifier { get; init; }

    [Option('r', "expectedRemaining", Default = 0.5, HelpText = "Weight for minimizing the average number of possible solutions left after the guess.")]
    public double ExpectedRemainingModifier { get; init; }

    [Option('w', "worstCase", Default = 0.3, HelpText = "Weight for minimizing the maximum number of possible solutions left in the least favorable feedback pattern.")]
    public double WorstCaseRemainingModifier { get; init; }

    [Option('g', "greenLetters", Default = 0.3, HelpText = "Weight given to expected green letters revealed by the guess and how soon those letters are revealed.")]
    public double GreenLetterModifier { get; init; }

    [Option('y', "yellowLetters", Default = 0.0, HelpText = "Weight given to expected yellow letters revealed by the guess and how soon those letters are revealed.")]
    public double YellowLetterModifier { get; init; }

    [Option('v', "vowelCount", Default = 0.3, HelpText = "Weight given to vowel coverage across the starting word set. Higher values favor words containing more vowels.")]
    public double VowelCountModifier { get; init; }

    [Option('d', "letterDistribution", Default = 0.1, HelpText = "Weight given to the distribution of characters in each word, and how soon they appear in the set.")]
    public double LetterDistributionOrderModifier { get; init; }

    [Option("requiredWords", Default = null, HelpText = "List of required words to include in the generated starting word set.")]
    public string? RequiredWords
    {
        get;

        init
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                field = null;
                return;
            }

            var     words   = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            short[] indexes = new short[words.Length];

            for (int i = 0; i < words.Length; i++)
            {
                var word  = words[i];
                var index = Data.ValidGuesses.IndexOf(word);

                if (index is -1)
                    throw new ArgumentException($"{word} is not a valid wordle guess and cannot be marked as required.", nameof(RequiredWords));

                indexes[i] = (short)index;
            }

            field                = value;
            RequiredWordsIndexes = indexes;
        }
    }

    public short[]? RequiredWordsIndexes { get; private init; }

    [Option("requiredLetters", Default = null, HelpText = "List of required letters to include in the generated starting word set.")]
    public string? RequiredLetters
    {
        get;

        init
        {
            if (value is null)
            {
                field = null;
                return;
            }

            RequiredLetterMask = 0;

            foreach (char c in value)
            {
                if (!char.IsLetter(c))
                    throw new ArgumentException($"`{c}` is not a valid letter to mark as required.", nameof(RequiredLetters));

                RequiredLetterMask |= 1 << (char.ToLower(c) - 'a');
            }

            field = value;
        }
    }

    public int? RequiredLetterMask { get; private init; }

    [Option("blockedLetters", Default = null, HelpText = "List of blocked letters to exclude in the generated starting word set.")]
    public string? BlockedLetters
    {
        get;

        init
        {
            if (value is null)
            {
                field = null;
                return;
            }

            BlockedLetterMask = 0;

            foreach (char c in value)
            {
                if (!char.IsLetter(c))
                    throw new ArgumentException($"`{c}` is not a valid letter to mark as blocked.", nameof(BlockedLetters));

                BlockedLetterMask |= 1 << (char.ToLower(c) - 'a');
            }

            field = value;
        }
    }

    public int? BlockedLetterMask { get; private init; }

    [Option("wordPattern", Default = null, HelpText = "A required pattern for words to be included in the generated starting word set (Use '*' to represent wildcards).")]
    public string? RequiredWordPattern
    {
        get;

        init
        {
            if (value is null)
            {
                field = null;
                return;
            }

            if (value.Length is not 5)
                throw new ArgumentException("Pattern must be 5 characters long.", nameof(RequiredWordPattern));

            if (value.Any(x => !char.IsAsciiLetter(x) && x is not '*'))
                throw new ArgumentException("Pattern must only consist of letters and wildcards.", nameof(RequiredWordPattern));

            field            = value.ToLower();
            RequiredWordMask = WordMask.FromMaskPattern(value.ToLower());
        }
    }

    public WordMask? RequiredWordMask { get; private init; }

    [Option("top", Default = 10, HelpText = "How many results to show when exporting data.")]
    public int TopResults { get; init; }

    [Option("verboseScoring", Default = false, HelpText = "Show all scoring information.")]
    public bool VerboseScoring { get; init; }

    [Option("showLetterGraph", Default = false, HelpText = "Show a graph showing the letter distribution of the sets.")]
    public bool ShowLetterDistributionGraph { get; init; }

    [Option("threads", HelpText = "How many system threads to use (0 will use processor count).")]
    public int ThreadCount { get; init => field = value <= 0 ? Environment.ProcessorCount : value; } = Environment.ProcessorCount;

    [Usage]
    public static IEnumerable<Example> Examples =>
    [
        new Example("Generate 2 starting words with default scoring modifiers",
                    new UnParserSettings() { PreferShortName = true, },
                    new Options() { SetSize = 2, }),

        new Example("Generate 4 starting words with higher focus on finding green letters, showing the top 20 results",
                    new UnParserSettings() { PreferShortName = true, },
                    new Options() { SetSize = 4, GreenLetterModifier = 1, EntropyModifier = .85, TopResults = 20, }),

        new Example("Generate 2 starting words that includes the letters `s` and `h` at the end",
                    new UnParserSettings() { PreferShortName = true, },
                    new Options() { SetSize = 2, RequiredWordPattern = "***sh", }),
    ];
}