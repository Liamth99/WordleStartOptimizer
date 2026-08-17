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

    public static Table BuildScoreTable(WordSet set, Options options)
    {
        var table = new Table();
        table.AddColumns("Metric", "Raw", "Normalized", "Weight", "Contribution");

        table.AddRow(
            "Entropy",
            $"{set.Entropy:N5}",
            ColorNormalizedScore(set.NormalizedEntropy),
            $"{options.EntropyModifier:N2}",
            $"{set.NormalizedEntropy * options.EntropyModifier:N3}");

        table.AddRow(
            "Expected Remaining",
            $"{set.ExpectedRemaining:N1}",
            ColorNormalizedScore(set.NormalizedExpectedRemaining),
            $"{options.ExpectedRemainingModifier:N2}",
            $"{set.NormalizedExpectedRemaining * options.ExpectedRemainingModifier:N3}");

        table.AddRow(
            "Worst Case Remaining",
            $"{set.WorstCaseRemaining:N0}",
            ColorNormalizedScore(set.NormalizedWorstCaseRemaining),
            $"{options.WorstCaseRemainingModifier:N2}",
            $"{set.NormalizedWorstCaseRemaining * options.WorstCaseRemainingModifier:N3}");

        table.AddRow(
            "Avg Green Letters",
            $"{set.AvgGreen:N2}",
            ColorNormalizedScore(set.NormalizedGreen),
            $"{options.GreenLetterModifier:N2}",
            $"{set.NormalizedGreen * options.GreenLetterModifier:N3}");

        table.AddRow(
            "Avg Yellow Letters",
            $"{set.AvgYellow:N2}",
            ColorNormalizedScore(set.NormalizedYellow),
            $"{options.YellowLetterModifier:N2}",
            $"{set.NormalizedYellow * options.YellowLetterModifier:N3}");

        table.AddRow(
            "Vowel Score",
            $"{set.VowelCount:N0}",
            ColorNormalizedScore(set.NormalizedVowelCount),
            $"{options.VowelCountModifier:N2}",
            $"{set.NormalizedVowelCount * options.VowelCountModifier:N3}");

        table.AddRow(
            "Letter Distribution Score",
            $"{set.LetterDistributionOrder:N3}",
            ColorNormalizedScore(set.NormalizedLetterDistributionOrder),
            $"{options.LetterDistributionOrderModifier:N2}",
            $"{set.NormalizedLetterDistributionOrder * options.LetterDistributionOrderModifier:N3}");

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