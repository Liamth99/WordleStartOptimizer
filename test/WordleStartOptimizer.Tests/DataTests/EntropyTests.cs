using WordleStartOptimizer.Utils;

namespace WordleStartOptimizer.Tests.DataTests;

public class EntropyTests
{
    [Fact]
    public void ValidWordsEntropiesAreCorrect()
    {
        Dictionary<byte, int> counts = new (Data.ValidGuesses.Length);
        for (int i = 0; i < Data.ValidGuesses.Length; i++)
        {
            counts.Clear();
            Data.WordEntropies[i].ShouldBe(
                i.AddPatternCounts(Enumerable.Range(0, Data.ValidGuesses.Length), counts)
                 .CalcEntropy(Data.ValidGuesses.Length),
                tolerance: 0.00001D);
        }
    }

    [Fact]
    public void ValidWordsAreOrderedByEntropy()
    {
        var indexes = Data.WordEntropies
                    .Select((e, i) => new { e, i })
                    .OrderByDescending(x => x.e)
                    .Select(x => x.i)
                    .ToArray();

        for (int i = 0; i < Data.ValidGuesses.Length; i++)
        {
            indexes[i].ShouldBe(i);
        }
    }
}