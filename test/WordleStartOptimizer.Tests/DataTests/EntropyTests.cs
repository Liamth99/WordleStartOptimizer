namespace WordleStartOptimizer.Tests.DataTests;

public class EntropyTests
{
    [Fact]
    public void ValidWordsAreOrderedByEntropy()
    {
        var indexes = Data.WordEntropies
                    .Select((e, i) => new { e, i })
                    .OrderByDescending(x => x.e)
                    .Select(x => x.i)
                    .ToArray();

        for (int i = 0; i < Data.ValidAnswers.Length; i++)
        {
            indexes[i].ShouldBe(i);
        }
    }
}