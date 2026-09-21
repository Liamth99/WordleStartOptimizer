using Spectre.Console;
using WordleStartOptimizer.Models;
using WordleStartOptimizer.Models.Options;

namespace WordleStartOptimizer.Output;

public static class SetMarkupBuilder
{
    public static string FormatWordSetMarkup(WordSet set)
        => string.Join(", ", set.Words.Select(x => $"[Aqua]{x}[/]"));

    public static Table BuildRawDataTable(WordSet set)
    {
        var table = new Table();

        table.AddColumns("Metric", "Value");
        table.AddRow("Entropy",                   $"{set.Entropy:N5}");
        table.AddRow("Expected Remaining",        $"{set.ExpectedRemaining:N1}");
        table.AddRow("Worst Case Remaining",      $"{set.WorstCaseRemaining:N0}");
        table.AddRow("Avg Green Letters",         $"{set.AvgGreen:N2}");
        table.AddRow("Avg Yellow Letters",        $"{set.AvgYellow:N2}");
        table.AddRow("Vowel Score",               $"{set.VowelScore:N2}");
        table.AddRow("Letter Distribution Score", $"{set.LetterDistributionOrder:N3}");
        table.AddRow("Valid Answers",             $"{set.ValidAnswers:N0}");

        return table;
    }

    public static Table BuildScoreTable(WordSet set, WordSetScoringContext context, SetGenerationOptions options)
    {
        var table = new Table();
        table.AddColumns("Metric", "Raw", "Normalized", "Weight", "Contribution");

        table.AddRow(
            "Entropy",
            $"{set.Entropy:N5}",
            ColorNormalizedScore(context.NormalizedEntropy(set)),
            $"{options.EntropyModifier:N2}",
            $"{context.NormalizedEntropy(set) * options.EntropyModifier:N3}");

        table.AddRow(
            "Expected Remaining",
            $"{set.ExpectedRemaining:N1}",
            ColorNormalizedScore(context.NormalizedExpectedRemaining(set)),
            $"{options.ExpectedRemainingModifier:N2}",
            $"{context.NormalizedExpectedRemaining(set) * options.ExpectedRemainingModifier:N3}");

        table.AddRow(
            "Worst Case Remaining",
            $"{set.WorstCaseRemaining:N0}",
            ColorNormalizedScore(context.NormalizedWorstCaseRemaining(set)),
            $"{options.WorstCaseRemainingModifier:N2}",
            $"{context.NormalizedWorstCaseRemaining(set) * options.WorstCaseRemainingModifier:N3}");

        table.AddRow(
            "Avg Green Letters",
            $"{set.AvgGreen:N2}",
            ColorNormalizedScore(context.NormalizedGreen(set)),
            $"{options.GreenLetterModifier:N2}",
            $"{context.NormalizedGreen(set) * options.GreenLetterModifier:N3}");

        table.AddRow(
            "Avg Yellow Letters",
            $"{set.AvgYellow:N2}",
            ColorNormalizedScore(context.NormalizedYellow(set)),
            $"{options.YellowLetterModifier:N2}",
            $"{context.NormalizedYellow(set) * options.YellowLetterModifier:N3}");

        table.AddRow(
            "Vowel Score",
            $"{set.VowelScore:N2}",
            ColorNormalizedScore(context.NormalizedVowelCount(set)),
            $"{options.VowelCountModifier:N2}",
            $"{context.NormalizedVowelCount(set)* options.VowelCountModifier:N3}");

        table.AddRow(
            "Letter Distribution Score",
            $"{set.LetterDistributionOrder:N3}",
            ColorNormalizedScore(context.NormalizedLetterDistributionOrder(set)),
            $"{options.LetterDistributionOrderModifier:N2}",
            $"{context.NormalizedLetterDistributionOrder(set) * options.LetterDistributionOrderModifier:N3}");

        table.AddRow(
            "Valid Answers",
            $"{set.ValidAnswers:N0}",
            " - ",
            " - ",
            " - ");

        return table;
    }

    public static Table BuildSetBreakDown(WordSet set, int[,] greenChances, int[,] yellowChances)
    {
        var maxColor = greenChances
                      .OfType<int>()
                      .Concat(yellowChances.OfType<int>())
                      .Max();

        var evalTable = new Table()
                            .AddColumn("Word",                      tb => tb.Centered())
                            .AddColumn("Color Heatmap",             tb => tb.Centered())
                            .AddColumn("Cumulative Avg Colors",     tb => tb.Centered())
                            .AddColumn("Word Entropy",              tb => tb.Centered())
                            .AddColumn("Cumulative Entropy",        tb => tb.Centered())
                            .AddColumn("Avg Words Remaining",       tb => tb.Centered())
                            .AddColumn("Remaining Words breakdown", tb => tb.Centered());

        double prevEntropy = 0;
        for (int guessIndex = 0; guessIndex < set.WordIndexes.Length; guessIndex++)
        {
            string guess = set.Words.ElementAt(guessIndex);
            var subSet   = set.SubSet(guessIndex);

            var colorHeatMap = new Canvas(5, 2);
            colorHeatMap.MaxWidth = 25;

            for (int i = 0; i < 5; i++)
            {
                var greenByte  = (byte)Math.Max(10, 255 * greenChances[guessIndex, i] / maxColor);
                var greenColor = new Color(0, greenByte, 0);
                colorHeatMap.SetPixel(i, 0, greenColor);

                var yellowByte  = (byte)Math.Max(10, 255 * yellowChances[guessIndex, i] / maxColor);
                var yellowColor = new Color(yellowByte, yellowByte, 0);
                colorHeatMap.SetPixel(i, 1, yellowColor);
            }

            var patternCounts = subSet.GetPatternCounts();
            var remainingAnswerChances = patternCounts
                   .Select(x => x.Value)
                   .GroupBy(x => x)
                   .Select(x => new { x.Key, Count = x.Count(), } )
                   .ToArray();

            var wordsRemainingBreakdown = new BreakdownChart().UseValueFormatter(x =>  x < 0.01 ? $"{x:P2}" : $"{x:P1}");

            foreach (var bucket in BuildBuckets(remainingAnswerChances.Max(x => x.Key)))
            {
                var answerGroupsInBucket = remainingAnswerChances.Where(x => x.Key <= bucket.max && x.Key >= bucket.min).ToArray();

                if(answerGroupsInBucket.Length is 0)
                    continue;

                var percentage = answerGroupsInBucket.Sum(x => x.Count * x.Key) / (double)Data.ValidGuesses.Length;

                wordsRemainingBreakdown.AddItem(bucket.label, percentage, bucket.color);
            }

            evalTable.AddRow(
                new Markup($"\n\n[cyan]{guess}[/]", new Style(decoration: Decoration.Bold | Decoration.Underline)),
                colorHeatMap,
                guessIndex is 0 ?
                    new Markup($"\n[green]{Data.GreenLetters[set.WordIndexes[guessIndex]]:N2}[/]\n\n[yellow]{Data.YellowLetters[set.WordIndexes[guessIndex]]:N2}[/]") :
                    new Markup($"\n[green]{subSet.AvgGreen:N2} (+ {Data.GreenLetters[set.WordIndexes[guessIndex]]:N2})[/]\n\n[yellow]{subSet.AvgYellow:N2} (+ {Data.YellowLetters[set.WordIndexes[guessIndex]]:N2})[/]"),
                new Markup($"\n\n{Data.WordEntropies[set.WordIndexes[guessIndex]]:N3}"),
                guessIndex is 0 ?
                    new Markup($"\n\n{subSet.Entropy:N3}") :
                    new Markup($"\n\n{subSet.Entropy:N3} [green]+{subSet.Entropy - prevEntropy:N3} (x {Math.Pow(2, subSet.Entropy - prevEntropy):N1})[/]"),
                new Markup($"\n\n{subSet.ExpectedRemaining:N2} ({subSet.WorstCaseRemaining:N0} max)"),
                wordsRemainingBreakdown
            );

            prevEntropy = subSet.Entropy;
        }

        return evalTable;
    }

    private static Color[] _bucketColors =
        [
            Color.Green,
            Color.Lime,
            Color.Yellow,
            Color.Orange1,
            Color.OrangeRed1,
            Color.DarkRed,
        ];

    private static (string label, int min, int max, Color color)[] BuildBuckets(int maxRemaining)
    {
        var buckets = new List<(string Label, int Min, int Max, Color color)> { ("1", 1, 1, _bucketColors[0]), };

        if (maxRemaining is 1)
            return buckets.ToArray();

        var remainingBucketCount = 6 - 1;
        var ratio = Math.Pow(maxRemaining / 2D, 1D / remainingBucketCount);

        var boundary = 2D;
        var prevMax  = 1;

        for (int i = 1; i < 6; i++)
        {
            int upper = i is 5 ? maxRemaining : (int)Math.Round(boundary);

            if (upper <= prevMax)
            {
                boundary *= ratio;
                continue;
            }

            string label = prevMax + 1 == upper ? $"{upper:N0}" : $"{prevMax + 1:N0}-{upper:N0}";
            buckets.Add((label, prevMax + 1, upper, _bucketColors[i]));

            prevMax  =  upper;
            boundary *= ratio;
        }

        return buckets.ToArray();
    }

    private static string ColorNormalizedScore(double s)
        => $"[{(s < .25 ? "red" : s < .75 ? "yellow" : "green")}]{s:N3}[/]";
}