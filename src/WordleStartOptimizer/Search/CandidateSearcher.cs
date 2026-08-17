using System.Collections.Concurrent;
using WordleStartOptimizer.Models;

namespace WordleStartOptimizer.Search;

public static class CandidateSearcher
{
    public static CandidateSet[] GenerateCandidates(Options options, Action<double>? onProgress = null)
    {
        ConcurrentBag<CandidateSet> candidates = [];

        int    completed    = 0;
        double lastProgress = 0;

        Lock progressLock = new();

        Parallel.For(
            0,
            Data.ProcessedGuesses.Length,
            new ParallelOptions { MaxDegreeOfParallelism = options.ThreadCount, },
            i =>
            {
                Interlocked.Increment(ref completed);

                StartSearchFrom(i, candidates, options);

                if(onProgress is null)
                    return;

                var progress = (double)completed / Data.ProcessedGuesses.Length;

                lock (progressLock)
                {
                    if (progress >= lastProgress + .01d)
                    {
                        lastProgress = progress;
                        onProgress(progress);
                    }
                }
            });

        return candidates.ToArray();
    }

    private static void StartSearchFrom(int firstWordIndex, ConcurrentBag<CandidateSet> candidates, Options options)
    {
        var mask = Data.ProcessedGuesses[firstWordIndex].LetterMask;

        if (mask < 0) // Word contains doubles and should not be included in a valid set
            return;


        var chosen = Enumerable.Repeat((short)0, options.SetSize).ToArray();
        chosen[0] = (short)firstWordIndex;

        int setIndex = 1;

        if (options.RequiredWordsIndexes is not null)
        {
            foreach (var wordIndex in options.RequiredWordsIndexes)
            {
                chosen[setIndex] = wordIndex;

                if((Data.ProcessedGuesses[wordIndex].LetterMask & mask) > 0) // Set would contain duplicate letters
                    return;

                mask |= Data.ProcessedGuesses[wordIndex].LetterMask;
                setIndex++;
            }
        }

        Search((short)(firstWordIndex + 1), mask, setIndex, chosen, candidates, options);
    }

    private static void Search(short start, int usedMask, int depth, short[] chosen, ConcurrentBag<CandidateSet> candidates, Options options)
    {
        if (depth == options.SetSize)
        {
            if(!MatchesConstraints(usedMask, chosen, options))
                return;

            double preScore = 0;
            var    words    = new short[options.SetSize];

            for (int i = 0; i < chosen.Length; i++)
            {
                preScore += Data.WordEntropies[chosen[i]];
                words[i] =  chosen[i];
            }

            candidates.Add(
                new CandidateSet()
                {
                    WordIndexes = words,
                    PreScore    = preScore,
                }
            );

            return;
        }

        for (short i = start; i < Data.ProcessedGuesses.Length; i++)
        {
            var mask = Data.ProcessedGuesses[i].LetterMask;

            if (mask < 0 || (usedMask & mask) is not 0)
                continue;

            chosen[depth] = i;

            Search((short)(i + 1), usedMask | mask, depth + 1, chosen, candidates, options);
        }
    }

    private static bool MatchesConstraints(int usedMask, short[] chosen, Options options)
    {
        if (options.RequiredLetterMask is not null && (options.RequiredLetterMask & usedMask) != options.RequiredLetterMask)
            return false;

        if (options.BlockedLetterMask is not null && (options.BlockedLetterMask & usedMask) > 0)
            return false;

        if (options.RequiredWordMask is not null && !options.RequiredWordMask.Value.AllCombinedMatchesPattern(chosen.Select(i => Data.ProcessedGuesses[i])))
            return false;

        return true;
    }
}