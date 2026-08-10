using System.Collections;
using WordleStartOptimizer.Models;

namespace WordleStartOptimizer;

public static partial class Data
{
    private static Lock _lock = new();

    public static WordInfo[] ProcessedGuesses = [];

    /// The counts of correct letters in their correct positions (green letters) for each possible guess.
    public static int[] GreenLetters = [];

    /// Tracks the count of yellow (misplaced but present) letters for each valid guess.
    public static int[] YellowLetters = [];

    /// Precomputed entropy values for each valid guess, calculated based on the
    /// distribution of response patterns to maximize information gain during gameplay.
    public static double[] WordEntropies = [];

    /// <summary>Feedback pattern for each answer/guess pair encoded in base 3.</summary>
    /// <remarks>indexed by guess then answer</remarks>
    public static byte[,] PatternMatrix = new byte[0, 0];

    /// Represents the theoretical maximum entropy achievable for the set of valid guesses.
    /// This value is calculated as the base-2 logarithm of the total number of valid guesses.
    /// Used as a reference for scaling and comparison of word entropies.
    public static double MaxPossibleEntropy = 0;

    /// Precomputed distribution of letter frequencies across all valid guesses.
    /// Represents the relative frequency of each letter in the dataset to assist
    /// in identifying high-value guesses during gameplay.
    public static Dictionary<char, double> LetterDistribution = Enumerable.Range(0, 26).Select(x => (char)('a' + x)).ToDictionary(x => x, _ => 0D);

    /// Represents a precalculated metric for each word based on the distribution of its letters in relation to their overall frequency in the word list.
    /// Higher values indicate words with letters that collectively appear more frequently across all valid guesses.
    /// This score is used to prioritize words that are expected to provide more informative feedback during
    public static double[] WordLetterDistributionScore = [];

    /// A bitmask that represents whether each word in the list of valid guesses is a valid answer.
    public static BitArray WordIsValidAnswer = null!;

    /// Represents the precomputed entropy contributions for various counts of word groups.
    /// Entropy contribution is calculated based on the probability of a group size relative
    /// to the total number of processed guesses and is used for entropy calculations in
    /// optimizing Wordle strategies.
    public static double[] EntropyContributionByCount = [];

    public static void Initialize()
    {
        ProcessedGuesses = ValidGuesses
                           .Select(word => new WordInfo(word))
                           .ToArray();

        GreenLetters  = new int[ProcessedGuesses.Length];
        YellowLetters = new int[ProcessedGuesses.Length];
        WordEntropies = new double[ProcessedGuesses.Length];

        PatternMatrix = new byte[ProcessedGuesses.Length, ProcessedGuesses.Length];

        MaxPossibleEntropy = Math.Log2(ValidGuesses.Length);

        WordLetterDistributionScore = new double[ProcessedGuesses.Length];
        WordIsValidAnswer           = new BitArray(ProcessedGuesses.Length);

        var denominator        = 1D / ProcessedGuesses.Length;
        var validAnswerHashSet = ValidAnswers.ToHashSet();

        Parallel.For(0,
                     ValidGuesses.Length,
                     new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = Program.Options.ThreadCount,
                    },
                     i =>
                     {
                         var guess = ProcessedGuesses[i];

                         foreach (char c in guess.Word)
                         {
                             lock (_lock)
                             {
                                 LetterDistribution[c] += denominator;
                             }
                         }

                         for (int j = 0; j < ProcessedGuesses.Length; j++)
                         {
                             byte code       = 0;
                             int  multiplier = 1;

                             WordInfo answer    = ProcessedGuesses[j];
                             int[]    remaining = new int[26];
                             int[]    states    = new int[5];

                             for (int k = 0; k < 5; k++)
                             {
                                 remaining[answer.Word[k] - 'a']++;
                             }

                             // Calc greens
                             for (int charI = 0; charI < 5; charI++)
                             {
                                 char c = guess.Word[charI];

                                 if (c == answer.Word[charI])
                                 {
                                     GreenLetters[i]++;
                                     states[charI] = 2;
                                     remaining[c - 'a']--;
                                 }
                             }

                             // Calc rest
                             for (int charI = 0; charI < 5; charI++)
                             {
                                 if(states[charI] is 2)
                                     continue;

                                 char c = guess.Word[charI];

                                 if (remaining[c - 'a'] > 0)
                                 {
                                     states[charI] = 1;
                                     YellowLetters[i]++;
                                     remaining[c - 'a']--;
                                 }
                                 else
                                 {
                                     states[charI] = 0;
                                 }
                             }

                             foreach (int state in states)
                             {
                                 code       += (byte)(state * multiplier);
                                 multiplier *= 3;
                             }

                             PatternMatrix[i, j] = code;
                         }

                         int[] patternCounts = new int[243];

                         for (int answerIndex = 0; answerIndex < ProcessedGuesses.Length; answerIndex++)
                         {
                             if(ProcessedGuesses[answerIndex].Mask < 0)
                                 continue;

                             int patternCode = PatternMatrix[i, answerIndex];
                             patternCounts[patternCode]++;
                         }

                         double entropy = 0;

                         foreach (int count in patternCounts)
                         {
                             if (count == 0)
                                 continue;

                             double probability = count / (double)ProcessedGuesses.Length;
                             entropy -= probability * Math.Log2(probability);
                         }

                         WordEntropies[i] = entropy;
                     });

        EntropyContributionByCount = new double[ProcessedGuesses.Length + 1];

        for (int i = 0; i < ValidGuesses.Length; i++)
        {
            var guess = ValidGuesses[i];
            WordLetterDistributionScore[i] = guess.Select(c => LetterDistribution[c]).Sum();

            if (validAnswerHashSet.Contains(guess))
                WordIsValidAnswer[i] = true;

            double probability = i / (double)ProcessedGuesses.Length;
            EntropyContributionByCount[i] = -probability * Math.Log2(probability);
        }
    }
}