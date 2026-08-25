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
        table.AddRow("Vowel Score",               $"{set.VowelCount:N0}");
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
            $"{set.VowelCount:N0}",
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
        double maxGreen = greenChances.OfType<int>().Max();
        double maxYellow = yellowChances.OfType<int>().Max();

        var breakDownTable = new Table()
                            .AddColumn("Word",               tb => tb.Centered())
                            .AddColumn("Color Heatmap",      tb => tb.Centered())
                            .AddColumn("Avg Colors",         tb => tb.Centered())
                            .AddColumn("Word Entropy",       tb => tb.Centered())
                            .AddColumn("Cumulative Entropy", tb => tb.Centered());

        double prevEntropy = 0;
        for (int guessIndex = 0; guessIndex < set.WordIndexes.Length; guessIndex++)
        {
            string guess   = set.Words.ElementAt(guessIndex);
            var    heatMap = new Canvas(5, 2);
            heatMap.MaxWidth = 25;
            var greenTotalCount = 0;
            var yellowTotalCount = 0;

            for (int i = 0; i < 5; i++)
            {
                var greenMultiple = greenChances[guessIndex, i] / maxGreen;
                var greenColor    = new Color(0, (byte)(128 * greenMultiple), 0);
                greenTotalCount   += greenChances[guessIndex, i];
                heatMap.SetPixel(i, 0, greenColor);

                var yellowMultiple = yellowChances[guessIndex, i] / maxYellow;
                var yellowColor    = new Color((byte)(255 * yellowMultiple), (byte)(255 * yellowMultiple), 0);
                yellowTotalCount    += yellowChances[guessIndex, i];
                heatMap.SetPixel(i, 1, yellowColor);
            }

            var currentEntropy = set.EntropyAtIndex(guessIndex);

            breakDownTable.AddRow(
                new Markup($"\n\n[cyan]{guess}[/]", new Style(decoration: Decoration.Bold | Decoration.Underline)),
                heatMap,
                new Markup($"\n[green]{greenTotalCount / (double)Data.ValidAnswers.Length:N2}[/]\n\n[yellow]{yellowTotalCount / (double)Data.ValidAnswers.Length:N2}[/]"),
                new Markup($"\n\n{Data.WordEntropies[set.WordIndexes[guessIndex]]:N3}"),
                guessIndex is 0 ? new Markup($"\n\n{currentEntropy:N3}") : new Markup($"\n\n{currentEntropy:N3} [green]+{currentEntropy - prevEntropy:N3} (x {Math.Pow(2, currentEntropy - prevEntropy):N1})[/]")
                );

            prevEntropy = currentEntropy;
        }

        return breakDownTable;
    }

    private static string ColorNormalizedScore(double s)
        => $"[{(s < .25 ? "red" : s < .75 ? "yellow" : "green")}]{s:N3}[/]";
}