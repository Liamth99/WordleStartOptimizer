using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WordleStartOptimizer.Models.Search.CandidateSet;

[DebuggerDisplay("{ToString()}")]
public readonly struct CandidateSet3 : ICandidateSet
{
    public override string ToString() => string.Join(", ", WordIndexes().Select(x => Data.ValidGuesses[x]));

    public CandidateSet3(ref short[] indexes)
    {
        _index1 = indexes[0];
        _index2 = indexes[1];
        _index3 = indexes[2];
    }

    public short WordIndex(int i)
    {
        return i switch
        {
            0 => _index1,
            1 => _index2,
            2 => _index3,
            _ => throw new IndexOutOfRangeException()
        };
    }

    public short[] WordIndexes() => [_index1, _index2, _index3, ];

    private readonly short _index1;
    private readonly short _index2;
    private readonly short _index3;
}