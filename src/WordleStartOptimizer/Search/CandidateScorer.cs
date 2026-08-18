using System.Collections.Concurrent;
using WordleStartOptimizer.Models;
using WordleStartOptimizer.Models.Options;

namespace WordleStartOptimizer.Search;

public static class CandidateScorer
{
    public static CandidateSet[] SelectCandidatesToScore(CandidateSet[] candidates, SetGenerationOptions options)
    {
        if (options.Effort is EffortLevel.Max)
            return candidates;

        int candidatesCheckingCount = int.Min(
            candidates.Length,
            int.Max(
                100_000,
                (int)Math.Round(candidates.Length * options.EffortPercentage, MidpointRounding.AwayFromZero))
            );

        return candidates
              .OrderByDescending(candidate => candidate.PreScore)
              .Take(candidatesCheckingCount)
              .ToArray();
    }

    public static WordSet[] ScoreCandidates(CandidateSet[] candidates, SetGenerationOptions options, Action<double>? onProgress = null)
    {
        ConcurrentBag<WordSet> scoredSets = [];

        int    completed    = 0;
        double lastReported = 0;
        Lock   progressLock = new();

        Parallel.ForEach(
            candidates,
            new ParallelOptions { MaxDegreeOfParallelism = options.ThreadCount, },
            candidate =>
            {
                scoredSets.Add(new WordSet(candidate.WordIndexes));

                Interlocked.Increment(ref completed);

                if (onProgress is null)
                    return;

                var progress = (double)completed / candidates.Length;

                lock (progressLock)
                {
                    if (progress >= lastReported + .01d)
                    {
                        lastReported = progress;
                        onProgress(progress);
                    }
                }
            });

        return scoredSets.ToArray();
    }
}