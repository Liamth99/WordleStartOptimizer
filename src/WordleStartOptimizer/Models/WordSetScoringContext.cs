using WordleStartOptimizer.Models.Options;

namespace WordleStartOptimizer.Models;

public sealed class WordSetScoringContext
{
    public MetricRange Entropy { get; }
    public MetricRange ExpectedRemaining { get; }
    public MetricRange WorstCaseRemaining { get; }
    public MetricRange Green { get; }
    public MetricRange Yellow { get; }
    public MetricRange LetterDistributionOrder { get; }

    public WordSetScoringContext(IReadOnlyCollection<WordSet> sets)
    {
        Entropy                 = MetricRange.Of(sets, x => x.Entropy);
        ExpectedRemaining       = MetricRange.Of(sets, x => x.ExpectedRemaining);
        WorstCaseRemaining      = MetricRange.Of(sets, x => x.WorstCaseRemaining);
        Green                   = MetricRange.Of(sets, x => x.AvgGreen);
        Yellow                  = MetricRange.Of(sets, x => x.AvgYellow);
        LetterDistributionOrder = MetricRange.Of(sets, x => x.LetterDistributionOrder);
    }

    public double NormalizedEntropy(WordSet set)
        => Entropy.Normalize(set.Entropy);

    public double NormalizedExpectedRemaining(WordSet set)
        => 1 - ExpectedRemaining.Normalize(set.ExpectedRemaining);

    public double NormalizedWorstCaseRemaining(WordSet set)
        => 1 - WorstCaseRemaining.Normalize(set.WorstCaseRemaining);

    public double NormalizedGreen(WordSet set)
        => Green.Normalize(set.AvgGreen);

    public double NormalizedYellow(WordSet set)
        => Yellow.Normalize(set.AvgYellow);

    public double NormalizedVowelCount(WordSet set)
        => set.VowelCount / 6D;

    public double NormalizedLetterDistributionOrder(WordSet set)
        => LetterDistributionOrder.Normalize(set.LetterDistributionOrder);

    public double Score(WordSet set, SetGenerationOptions options) =>
        options.EntropyModifier                 * NormalizedEntropy(set) +
        options.ExpectedRemainingModifier       * NormalizedExpectedRemaining(set) +
        options.WorstCaseRemainingModifier      * NormalizedWorstCaseRemaining(set) +
        options.GreenLetterModifier             * NormalizedGreen(set) +
        options.YellowLetterModifier            * NormalizedYellow(set) +
        options.VowelCountModifier              * NormalizedVowelCount(set) +
        options.LetterDistributionOrderModifier * NormalizedLetterDistributionOrder(set);

    public readonly struct MetricRange
    {
        public double Min { get; }
        public double Max { get; }

        private MetricRange(double min, double max)
        {
            Min = min;
            Max = max;
        }

        public static MetricRange Of(IReadOnlyCollection<WordSet> sets, Func<WordSet, double> selector)
        {
            if (sets.Count is 0)
                return new MetricRange(0, 0);

            double min = double.MaxValue;
            double max = double.MinValue;

            foreach (var set in sets)
            {
                var value = selector(set);

                if (value < min) min = value;
                if (value > max) max = value;
            }

            return new MetricRange(min, max);
        }

        internal double Normalize(double value) => (value - Min) / (Max - Min);
    }
}