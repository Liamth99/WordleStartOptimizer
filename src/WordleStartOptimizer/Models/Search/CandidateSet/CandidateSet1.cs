using System.Diagnostics;

namespace WordleStartOptimizer.Models.Search.CandidateSet;

[DebuggerDisplay("{ToString()}")]
public readonly struct CandidateSet1 : ICandidateSet
{
    public override string ToString() => string.Join(", ", WordIndexes().Select(x => Data.ValidGuesses[x]));

    public CandidateSet1(ref short[] indexes)
    {
        _index1 = indexes[0];
    }

    public short WordIndex(int i)
    {
        return i switch
        {
            0 => _index1,
            _ => throw new IndexOutOfRangeException()
        };
    }

    public short[] WordIndexes() => [_index1, ];

    private readonly short _index1;
}