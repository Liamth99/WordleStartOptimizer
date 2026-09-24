using System.Numerics;

namespace WordleStartOptimizer.Utils;

public static class EntropyUtils
{
    extension(IDictionary<byte, int> patternCounts)
    {
        public (double entropy, int worstRemaining) CalcEntropyWithWorstRemaining<TNumber>(TNumber sampleSize)
            where TNumber : INumber<TNumber>
        {
            double entropy        = 0;
            int    worstRemaining = 0;

            foreach (int patternCount in patternCounts.Values)
            {
                var probability        = patternCount / (sampleSize is double d ? d : double.CreateChecked(sampleSize));
                entropy -= probability * Math.Log2(probability);

                if (worstRemaining < patternCount)
                    worstRemaining = patternCount;
            }

            return (entropy, worstRemaining);
        }

        public double CalcEntropy<TNumber>(TNumber sampleSize)
            where TNumber : INumber<TNumber>
        {
            double entropy = 0;

            foreach (int patternCount in patternCounts.Values)
            {
                double probability        = patternCount / (sampleSize is double d ? d : double.CreateChecked(sampleSize));
                entropy -= probability * Math.Log2(probability);
            }

            return entropy;
        }

        public double CalcWorstRemaining()
        {
            int worstRemaining = 0;

            foreach (int patternCount in patternCounts.Values)
            {
                if (worstRemaining < patternCount)
                    worstRemaining = patternCount;
            }

            return worstRemaining;
        }
    }
}