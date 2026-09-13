using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace WordleStartOptimizer.Models;

[DebuggerDisplay("{DisplayString}")]
public readonly struct CandidateSet
{
    private string DisplayString => string.Join(", ", WordIndexes().Select(x => Data.ValidGuesses[x]));

    [SetsRequiredMembers]
    public CandidateSet(IEnumerable<short> indexes, double preScore)
    {
        var indexArr = indexes as short[] ?? indexes.ToArray();
        PreScore         = preScore;
        AggregatedIndexes = 0;

        for (int i = 0; i < 8; i++)
        {
            if(i < indexArr.Length)
                AggregatedIndexes |= (Int128)indexArr[i] << (i * 16);
            else
                AggregatedIndexes |= (Int128)short.MaxValue << (i * 16);
        }
    }

    public short WordIndex(int i) => (short)(AggregatedIndexes >> i * 16);

    public short[] WordIndexes()
    {
        List<short> results = [];
        for (int i = 0; i < 8; i++)
        {
            var index = WordIndex(i);

            if (index is short.MaxValue)
                break;

            results.Add(index);
        }

        return results.ToArray();
    }

    public required Int128 AggregatedIndexes { get; init; }
    public required double PreScore { get; init; }
}