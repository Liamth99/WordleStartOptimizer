using System.Diagnostics;

namespace WordleStartOptimizer.Models;

[DebuggerDisplay("{DisplayString}")]
public readonly struct CandidateSet
{
    private string DisplayString => string.Join(", ", WordIndexes.Select(x => Data.ValidGuesses[x]));

    public required short[]  WordIndexes { get; init; }
    public required double PreScore    { get; init; }
}