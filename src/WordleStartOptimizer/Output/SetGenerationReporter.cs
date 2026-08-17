using Spectre.Console;
using WordleStartOptimizer.Models;

namespace WordleStartOptimizer.Output;

public static class SetGenerationReporter
{
    public static void ReportNoResults()
    {
        AnsiConsole.MarkupLine("[red]Found no valid sets with current configuration.[/]");
    }

    public static void ReportSingleResult(WordSet set, Options options)
    {
        AnsiConsole.MarkupLine("[Yellow]Only found one valid set with configuration.[/]");
        AnsiConsole.MarkupLine(SetMarkupBuilder.FormatWordSetMarkup(set));

        if(options.VerboseScoring)
            AnsiConsole.Write(SetMarkupBuilder.BuildRawDataTable(set));
    }

    public static void ReportTopResults(WordSet[] bestResults, Options options)
    {
        AnsiConsole.Write(new Rule($"Top {bestResults.Length} results").LeftJustified());

        for (int i = 0; i < bestResults.Length; i++)
        {
            var set = bestResults[i];

            AnsiConsole.MarkupLine($"{i + 1}: {SetMarkupBuilder.FormatWordSetMarkup(set)}");
            AnsiConsole.MarkupLine($"Total Score: [green]{set.Score:N3}[/]");

            if (options.VerboseScoring)
                AnsiConsole.Write(SetMarkupBuilder.BuildScoreTable(set, options));

            AnsiConsole.WriteLine();
        }
    }

    public static void ReportGlobalStats()
    {
        AnsiConsole.Write(new Rule("Global Stat Breakdown").LeftJustified());
        AnsiConsole.MarkupLine($"Entropy Theoretical max [green]{Data.MaxPossibleEntropy:N5}[/]");
        AnsiConsole.MarkupLine($"Entropy: [yellow]{WordSet.MinEntropy:N5}[/] -> [green]{WordSet.MaxEntropy:N5}[/]");
        AnsiConsole.MarkupLine($"Expected Remaining: [yellow]{WordSet.MaxExpectedRemaining:N1}[/] -> [green]{WordSet.MinExpectedRemaining:N1}[/]");
        AnsiConsole.MarkupLine($"Worst Case Remaining: [yellow]{WordSet.MaxWorstCaseRemaining:N0}[/] -> [green]{WordSet.MinWorstCaseRemaining:N0}[/]");
        AnsiConsole.MarkupLine($"Avg Greens: [yellow]{WordSet.MinGreen:N2}[/] -> [green]{WordSet.MaxGreen:N2}[/]");
        AnsiConsole.MarkupLine($"Avg Yellows: [yellow]{WordSet.MinYellow:N2}[/] -> [green]{WordSet.MaxYellow:N2}[/]");
        AnsiConsole.MarkupLine($"Letter Distribution Order: [yellow]{WordSet.MinLetterDistributionOrder:N1}[/] -> [green]{WordSet.MaxLetterDistributionOrder:N1}[/]");
        AnsiConsole.WriteLine();
    }
}