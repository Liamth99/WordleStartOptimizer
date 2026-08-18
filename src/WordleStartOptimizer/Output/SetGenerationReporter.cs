using Spectre.Console;
using WordleStartOptimizer.Models;
using WordleStartOptimizer.Models.Options;

namespace WordleStartOptimizer.Output;

public static class SetGenerationReporter
{
    public static void ReportNoResults()
    {
        AnsiConsole.MarkupLine("[red]Found no valid sets with current configuration.[/]");
    }

    public static void ReportSingleResult(WordSet set, SetGenerationOptions options)
    {
        AnsiConsole.MarkupLine("[Yellow]Only found one valid set with configuration.[/]");
        AnsiConsole.MarkupLine(SetMarkupBuilder.FormatWordSetMarkup(set));

        if(options.VerboseScoring)
            AnsiConsole.Write(SetMarkupBuilder.BuildRawDataTable(set));
    }

    public static void ReportTopResults(WordSet[] bestResults, WordSetScoringContext context, SetGenerationOptions options)
    {
        AnsiConsole.Write(new Rule($"Top {bestResults.Length} results").LeftJustified());

        for (int i = 0; i < bestResults.Length; i++)
        {
            var set = bestResults[i];

            AnsiConsole.MarkupLine($"{i + 1}: {SetMarkupBuilder.FormatWordSetMarkup(set)}");
            AnsiConsole.MarkupLine($"Total Score: [green]{context.Score(set, options):N3}[/]");

            if (options.VerboseScoring)
                AnsiConsole.Write(SetMarkupBuilder.BuildScoreTable(set, context, options));

            AnsiConsole.WriteLine();
        }
    }

    public static void ReportGlobalStats(WordSetScoringContext context)
    {
        AnsiConsole.Write(new Rule("Global Stat Breakdown").LeftJustified());
        AnsiConsole.MarkupLine($"Entropy Theoretical max [green]{Data.MaxPossibleEntropy:N5}[/]");
        PrintRange("Entropy", context.Entropy, "N5");
        PrintRange("Expected Remaining", context.ExpectedRemaining, "N1", invert: true);
        PrintRange("Worst Case Remaining", context.WorstCaseRemaining, "N0", invert: true);
        PrintRange("Avg Greens", context.Green, "N2");
        PrintRange("Avg Yellows", context.Yellow, "N2");
        PrintRange("Letter Distribution Order", context.LetterDistributionOrder, "N1");
        AnsiConsole.WriteLine();
    }

    private static void PrintRange(string label, WordSetScoringContext.MetricRange range, string format, bool invert = false)
    {
        var (low, high) = invert ? (range.Max, range.Min) : (range.Min, range.Max);
        AnsiConsole.MarkupLine($"{label}: [yellow]{low.ToString(format)}[/] -> [green]{high.ToString(format)}[/]");
    }
}