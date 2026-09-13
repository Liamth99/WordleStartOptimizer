using System.Diagnostics;

namespace WordleStartOptimizer.Models;

[DebuggerDisplay("{DisplayString}")]
public readonly struct CandidateSet
{
    private string DisplayString => string.Join(", ", WordIndexes().Select(x => Data.ValidGuesses[x]));

    public CandidateSet(IEnumerable<short> indexes)
    {
        var indexArr = indexes as short[] ?? indexes.ToArray();

        _index1 = indexArr.Length >= 1 ? indexArr[0] : short.MaxValue;
        _index2 = indexArr.Length >= 2 ? indexArr[1] : short.MaxValue;
        _index3 = indexArr.Length >= 3 ? indexArr[2] : short.MaxValue;
        _index4 = indexArr.Length >= 4 ? indexArr[3] : short.MaxValue;
        _index5 = indexArr.Length >= 5 ? indexArr[4] : short.MaxValue;
    }

    public short WordIndex(int i)
    {
        switch (i)
        {
            case 0:
                return _index1;
            case 1:
                return _index2;
            case 2:
                return _index3;
            case 3:
                return _index4;
            case 4:
                return _index5;
            default:
                throw new IndexOutOfRangeException();
        }
    }

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

    private readonly short _index1;
    private readonly short _index2;
    private readonly short _index3;
    private readonly short _index4;
    private readonly short _index5;
}