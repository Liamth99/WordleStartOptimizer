using Spectre.Console;
using WordleStartOptimizer.Models;

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

    public static Table BuildScoreTable(WordSet set, WordSetScoringContext context, Options options)
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

    private static string ColorNormalizedScore(double s)
        => $"[{(s < .25 ? "red" : s < .75 ? "yellow" : "green")}]{s:N3}[/]";
}