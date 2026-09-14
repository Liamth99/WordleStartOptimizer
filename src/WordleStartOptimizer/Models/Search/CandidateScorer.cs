using System.Collections.Concurrent;
using WordleStartOptimizer.Models.Options;

namespace WordleStartOptimizer.Models.Search;

public static class CandidateScorer
{
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
                scoredSets.Add(new WordSet(candidate.WordIndexes()));

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