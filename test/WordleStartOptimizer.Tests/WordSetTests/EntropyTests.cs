namespace WordleStartOptimizer.Tests.WordSetTests;

public class EntropyTests
{
    [Theory]
    [InlineData("crane")]
    [InlineData("tares")]
    [InlineData("lares")]
    [InlineData("rales")]
    [InlineData("rates")]
    public void WordEntropy(string word)
    {
        var wordIndex = Data.ValidGuesses.IndexOf(word);
        var set       = new WordSet([(short)wordIndex]);

        var entropy = CalculateEntropy([word]);

        set.Entropy.ShouldBe(entropy, 0.00001);
        Data.WordEntropies[wordIndex].ShouldBe(entropy, 0.00001);
    }

    [Theory]
    [InlineData("crane", "slate")]
    public void WordSetEntropy(params string[] words)
    {
        var wordIndexes = words.Select(x => (short)Data.ValidGuesses.IndexOf(x)).ToArray();
        var set         = new WordSet(wordIndexes);

        var entropy = CalculateEntropy(words);

        set.Entropy.ShouldBe(entropy, 0.00001);
    }

    [Theory]
    [InlineData("crane")]
    [InlineData("tares")]
    [InlineData("lares")]
    [InlineData("rales")]
    [InlineData("rates")]
    public void DuplicateWordsDontAffectEntropy(string word)
    {
        var singleE = CalculateEntropy([word]);
        var doubleE = CalculateEntropy([word, word]);
        var tripleE = CalculateEntropy([word, word, word]);

        singleE.ShouldBe(doubleE);
        singleE.ShouldBe(tripleE);
    }

    [Theory]
    [InlineData("crane", "tares")]
    [InlineData("crane", "slate")]
    [InlineData("boats", "chess")]
    public void EntropyIsOrderAgnostic(string word1, string word2)
    {
        var set1 = new WordSet([(short)Data.ValidGuesses.IndexOf(word1), (short)Data.ValidGuesses.IndexOf(word2)]);
        var set2 = new WordSet([(short)Data.ValidGuesses.IndexOf(word2), (short)Data.ValidGuesses.IndexOf(word1)]);

        set1.Entropy.ShouldBe(set2.Entropy);
    }

    private double CalculateEntropy(string[] words)
    {
        var wordIndexes = words.Select(x => (short)Data.ValidGuesses.IndexOf(x)).ToArray();

        Dictionary<long, int> patternCounts = new();

        for (int answerIndex = 0; answerIndex < Data.ProcessedGuesses.Length; answerIndex++)
        {
            long combinedPatternCode = 0;
            long multiplier          = 1;

            foreach (int guessIndex in wordIndexes)
            {
                combinedPatternCode += Data.PatternMatrix[guessIndex, answerIndex] * multiplier;
                multiplier          *= 243;
            }

            if (!patternCounts.TryAdd(combinedPatternCode, 1))
            {
                patternCounts[combinedPatternCode]++;
            }
        }

        double entropy = 0;

        foreach (int count in patternCounts.Values)
        {
            if (count is 0)
                continue;

            double probability = count / (double)Data.ProcessedGuesses.Length;
            entropy -= probability * Math.Log2(probability);
        }

        return entropy;
    }
}