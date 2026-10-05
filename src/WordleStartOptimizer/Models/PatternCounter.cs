using System.Collections;
using System.Diagnostics;
using System.Numerics;

namespace WordleStartOptimizer.Models;

[DebuggerDisplay("Keys = {_keys.Length}")]
public sealed class PatternCounter<TPattern> : IEnumerable<KeyValuePair<TPattern,short>>
    where TPattern : IBinaryInteger<TPattern>, IUnsignedNumber<TPattern>
{
    private readonly TPattern[] _keys;
    private readonly short[]    _counts;
    private readonly short[]    _indexedKeys;
    private readonly int        _mask;
    private readonly int        _shift;
    private          int        _usedCount;

    public PatternCounter(int maxItems)
    {
        int size = (int)BitOperations.RoundUpToPowerOf2((uint)maxItems * 2);

        _keys        = new TPattern[size];
        _counts      = new short[size];
        _indexedKeys = new short[size];
        _mask        = size - 1;
        _shift       = 64 - BitOperations.Log2((uint)size);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<KeyValuePair<TPattern, short>> GetEnumerator()
    {
        for (int i = 0; i < _usedCount; i++)
            yield return new KeyValuePair<TPattern, short>(_keys[_indexedKeys[i]], _counts[_indexedKeys[i]]);
    }

    public IEnumerator<TPattern> Keys
    {
        get
        {
            for (int i = 0; i < _usedCount; i++)
                yield return _keys[_indexedKeys[i]];
        }
    }

    public IEnumerator<int> Values
    {
        get
        {
            for (int i = 0; i < _usedCount; i++)
                yield return _counts[_indexedKeys[i]];
        }
    }

    public void Add(TPattern key)
    {
        short slot = (short)((ulong.CreateTruncating(key) * 0x9E3779B97F4A7C15UL) >> _shift);

        while (true)
        {
            if (_counts[slot] == 0)
            {
                _keys[slot]                = key;
                _counts[slot]              = 1;
                _indexedKeys[_usedCount++] = slot;
                return;
            }

            if (_keys[slot] == key)
            {
                _counts[slot]++;
                return;
            }

            slot = (short)((slot + 1) & _mask);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < _usedCount; i++)
            _counts[_indexedKeys[i]] = 0;
        _usedCount = 0;
    }

    public double CalculateEntropy<TNumber>(TNumber sampleSize) where TNumber : INumber<TNumber>
    {
        double entropy = 0;

        if (int.CreateTruncating(sampleSize) == Data.ValidAnswers.Length)
        {
            for (int i = 0; i < _usedCount; i++)
            {
                int count = _counts[_indexedKeys[i]];
                if (count is 0)
                    continue;

                entropy += Data.EntropyContributionByCount[count];
            }
            return entropy;
        }

        for (int i = 0; i < _usedCount; i++)
        {
            int count = _counts[_indexedKeys[i]];
            if (count is 0)
                continue;

            double probability = count / double.CreateChecked(sampleSize);
            entropy -= probability * Math.Log2(probability);
        }
        return entropy;
    }

    public (double entropy, int worstCase) CalculateEntropyWithWorstRemaining<TNumber>(TNumber sampleSize) where TNumber : INumber<TNumber>
    {
        double entropy   = 0;
        int    worstCase = 0;

        if (int.CreateTruncating(sampleSize) == Data.ValidAnswers.Length)
        {
            for (int i = 0; i < _usedCount; i++)
            {
                int count = _counts[_indexedKeys[i]];
                if (count is 0)
                    continue;

                entropy += Data.EntropyContributionByCount[count];

                if (worstCase < count)
                    worstCase = count;
            }
            return (entropy, worstCase);
        }

        for (int i = 0; i < _usedCount; i++)
        {
            int count = _counts[_indexedKeys[i]];
            if (count is 0)
                continue;

            double probability = count / double.CreateChecked(sampleSize);
            entropy -= probability * Math.Log2(probability);

            if (worstCase < count)
                worstCase = count;
        }
        return (entropy, worstCase);
    }

    public (double entropy, int worstCase, double expectedRemaining) CalculateStatistics<TNumber>(TNumber sampleSize) where TNumber : INumber<TNumber>
    {
        double total                = double.CreateChecked(sampleSize);
        double entropy              = 0;
        int    worstCase            = 0;
        int    expectedRemainingSum = 0;

        if (int.CreateTruncating(sampleSize) == Data.ValidAnswers.Length)
        {
            for (int i = 0; i < _usedCount; i++)
            {
                int count = _counts[_indexedKeys[i]];
                if (count is 0)
                    continue;

                entropy += Data.EntropyContributionByCount[count];

                expectedRemainingSum += count * count;

                if (worstCase < count)
                    worstCase = count;
            }
            return (entropy, worstCase, expectedRemainingSum / total);
        }

        for (int i = 0; i < _usedCount; i++)
        {
            int count = _counts[_indexedKeys[i]];
            if (count is 0)
                continue;

            double probability = count / double.CreateChecked(sampleSize);
            entropy -= probability * Math.Log2(probability);

            expectedRemainingSum += count * count;

            if (worstCase < count)
                worstCase = count;
        }
        return (entropy, worstCase, expectedRemainingSum / total);
    }

    public int CalculateWorstRemaining()
    {
        return _indexedKeys.Take(_usedCount).Max(i => _counts[i]);
    }
}