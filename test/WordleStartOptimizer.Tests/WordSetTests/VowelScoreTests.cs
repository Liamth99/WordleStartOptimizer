namespace WordleStartOptimizer.Tests.WordSetTests;

public class VowelScoreTests
{
    [Theory]
    [InlineData("ae",    "crane")]
    [InlineData("ae",    "crane", "slate")]
    [InlineData("y",     "gymps")]
    [InlineData("a",     "yawny")]
    [InlineData("u",     "punky")]
    [InlineData("oaeiu", "borts", "gamed", "filch", "punky")]
    [InlineData("aeoiu", "sared", "compt", "whilk", "bungy")]
    public void VowelScoreCorrect(string expectedVowels, params string[] words)
    {
        var wordIndexes = words.Select(x => (short)Data.ValidGuesses.IndexOf(x)).ToArray();
        var set         = new WordSet(wordIndexes);

        set.VowelScore.ShouldBe(expectedVowels.Sum(x => x is 'y' ? Data.YAsVowelDistributionScore : Data.LetterDistribution[x]) / Data.TotalVowelLetterDistributionScore);
    }
}