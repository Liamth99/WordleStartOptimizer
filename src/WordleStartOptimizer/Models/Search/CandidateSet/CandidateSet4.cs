using System.Diagnostics;

namespace WordleStartOptimizer.Models.Search.CandidateSet;

[DebuggerDisplay("{ToString()}")]
public readonly struct CandidateSet4 : ICandidateSet
{
    public override string ToString() => string.Join(", ", WordIndexes().Select(x => Data.ValidGuesses[x]));

    public CandidateSet4(ref short[] indexes)
    {
        _index1 = indexes[0];
        _index2 = indexes[1];
        _index3 = indexes[2];
        _index4 = indexes[3];
    }

    public short WordIndex(int i)
    {
        return i switch
        {
            0 => _index1,
            1 => _index2,
            2 => _index3,
            3 => _index4,
            _ => throw new IndexOutOfRangeException()
        };
    }

    public short[] WordIndexes() => [_index1, _index2, _index3, _index4, ];

    private readonly short _index1;
    private readonly short _index2;
    private readonly short _index3;
    private readonly short _index4;
}