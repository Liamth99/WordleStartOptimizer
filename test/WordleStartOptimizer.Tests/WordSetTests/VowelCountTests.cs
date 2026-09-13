namespace WordleStartOptimizer.Tests.WordSetTests;

public class VowelCountTests
{
    [Theory]
    [InlineData(2, "crane")]
    [InlineData(2, "crane", "slate")]
    [InlineData(1, "gymps")]
    [InlineData(1, "yawny")]
    public void VowelCountCorrect(int expectedCount, params string[] words)
    {
        var wordIndexes = words.Select(x => (short)Data.ValidGuesses.IndexOf(x)).ToArray();
        var set         = new WordSet(wordIndexes);

        set.VowelCount.ShouldBe(expectedCount);
    }
}