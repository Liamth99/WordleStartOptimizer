namespace WordleStartOptimizer.Models.Search.CandidateSet;

public interface ICandidateSet
{
    short   WordIndex(int i);
    short[] WordIndexes();

    public static ICandidateSet CreateSet(short[] indexes)
    {
        return indexes.Length switch
        {
            1 => new CandidateSet1(ref indexes),
            2 => new CandidateSet2(ref indexes),
            3 => new CandidateSet3(ref indexes),
            4 => new CandidateSet4(ref indexes),
            5 => new CandidateSet5(ref indexes),
            _ => throw new IndexOutOfRangeException()
        };
    }
}