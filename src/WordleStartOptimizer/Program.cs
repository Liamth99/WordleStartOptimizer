using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Nodes;
using CommandLine;
using WordleStartOptimizer.Models;
using Spectre.Console;
using WordleStartOptimizer.Search;

namespace WordleStartOptimizer;

internal class Program
{
    public static  Options Options { get; private set; } = null!;

    public static async Task Main(string[] args)
    {
        var parserResult = Parser.Default.ParseArguments<Options>(args);

        if(parserResult.Errors.Any())
            return;

        Options = parserResult.Value;

        if (Options.RequiredWordsIndexes?.Length >= Options.SetSize)
        {
            AnsiConsole.WriteException(new ArgumentException($"Required words length must be less than Set Size ({Options.SetSize}).", nameof(Options.RequiredWords)));
            return;
        }

        await VersionChecker.CheckVersionAsync();

        AnsiConsole.MarkupLine($"Generating starting word sets with {Options.SetSize} words.");
        AnsiConsole.MarkupLine($"Using [red]{Options.ThreadCount}[/] threads.");
        if (Options.RequiredWordsIndexes is not null)
        {
            AnsiConsole.MarkupLine($"Using required words: {string.Join(", ", Options.RequiredWordsIndexes.Select(x => $"[cyan]{Data.ValidGuesses[x]}[/]"))}");
        }
        if (Options.RequiredLetters is not null)
        {
            AnsiConsole.MarkupLine($"Using required letters: {string.Join(", ", Options.RequiredLetters.Select(x => $"[cyan]{x}[/]"))}");
        }
        if (Options.BlockedLetters is not null)
        {
            AnsiConsole.MarkupLine($"Excluding blocked letters: {string.Join(", ", Options.BlockedLetters.Select(x => $"[red]{x}[/]"))}");
        }

        Data.Initialize(Options.ThreadCount);

        var scoredSets = RunSearch(Options);

        if (scoredSets.Length is 0)
        {
            AnsiConsole.MarkupLine("[red]Found no valid sets with current configuration.[/]");
            return;
        }

        if (scoredSets.Length is 1)
        {
            var set = scoredSets.First();

            AnsiConsole.MarkupLine("[Yellow]Only found one valid set with configuration.[/]");
            AnsiConsole.MarkupLine($"{string.Join(", ", set.Words.Select(x => $"[Aqua]{x}[/]") )}");

            if (Options.VerboseScoring)
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

                AnsiConsole.Write(table);
            }

            return;
        }

        if (Options.VerboseScoring)
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

        WordSet[] bestResults = scoredSets
                         .OrderByDescending(set => set.Score)
                         .Take(Options.TopResults)
                         .ToArray();

        AnsiConsole.Write(new Rule($"Top {bestResults.Length} results").LeftJustified());

        for (int i = 0; i < bestResults.Length; i++)
        {
            WordSet set = bestResults[i];

            AnsiConsole.MarkupLine($"{i + 1}: {string.Join(", ", set.Words.Select(x => $"[Aqua]{x}[/]") )}");
            AnsiConsole.MarkupLine($"Total Score: [green]{set.Score:N3}[/]");

            if (Options.VerboseScoring)
            {
                var table = new Table();

                table.AddColumns(
                    "Metric",
                    "Raw",
                    "Normalized",
                    "Weight",
                    "Contribution");

                table.AddRow(
                    "Entropy",
                    $"{set.Entropy:N5}",
                    ColorNormalizedScore(set.NormalizedEntropy),
                    $"{Options.EntropyModifier:N2}",
                    $"{set.NormalizedEntropy * Options.EntropyModifier:N3}");

                table.AddRow(
                    "Expected Remaining",
                    $"{set.ExpectedRemaining:N1}",
                    ColorNormalizedScore(set.NormalizedExpectedRemaining),
                    $"{Options.ExpectedRemainingModifier:N2}",
                    $"{set.NormalizedExpectedRemaining * Options.ExpectedRemainingModifier:N3}");

                table.AddRow(
                    "Worst Case Remaining",
                    $"{set.WorstCaseRemaining:N0}",
                    ColorNormalizedScore(set.NormalizedWorstCaseRemaining),
                    $"{Options.WorstCaseRemainingModifier:N2}",
                    $"{set.NormalizedWorstCaseRemaining * Options.WorstCaseRemainingModifier:N3}");

                table.AddRow(
                    "Avg Green Letters",
                    $"{set.AvgGreen:N2}",
                    ColorNormalizedScore(set.NormalizedGreen),
                    $"{Options.GreenLetterModifier:N2}",
                    $"{set.NormalizedGreen * Options.GreenLetterModifier:N3}");

                table.AddRow(
                    "Avg Yellow Letters",
                    $"{set.AvgYellow:N2}",
                    ColorNormalizedScore(set.NormalizedYellow),
                    $"{Options.YellowLetterModifier:N2}",
                    $"{set.NormalizedYellow * Options.YellowLetterModifier:N3}");

                table.AddRow(
                    "Vowel Score",
                    $"{set.VowelCount:N0}",
                    ColorNormalizedScore(set.NormalizedVowelCount),
                    $"{Options.VowelCountModifier:N2}",
                    $"{set.NormalizedVowelCount * Options.VowelCountModifier:N3}");

                table.AddRow(
                    "Letter Distribution Score",
                    $"{set.LetterDistributionOrder:N3}",
                    ColorNormalizedScore(set.NormalizedLetterDistributionOrder),
                    $"{Options.LetterDistributionOrderModifier:N2}",
                    $"{set.NormalizedLetterDistributionOrder * Options.LetterDistributionOrderModifier:N3}");

                table.AddRow(
                    "Valid Answers",
                    $"{set.ValidAnswers:N0}",
                    " - ",
                    " - ",
                    " - ");

                AnsiConsole.Write(table);
            }

            if (Options.ShowLetterDistributionGraph)
            {
                var letters = set.Words.SelectMany(x => x).ToArray();

                var charts =
                    Data.LetterDistribution
                        .OrderByDescending(x => x.Value)
                        .Chunk(9)
                        .Select(chunk =>
                                {
                                    return new BarChart()
                                          .Width(Console.BufferWidth / 4)
                                          .UseValueFormatter(v => $"{v:P1}")
                                          .AddItems(chunk, pair => new BarChartItem(pair.Key.ToString(), pair.Value, letters.Contains(pair.Key) ? Color.Green : Color.Grey));
                                });

                AnsiConsole.Write(new Columns(charts));
            }

            AnsiConsole.WriteLine();
        }
    }

    private static string ColorNormalizedScore(double s)
    {
        return $"[{(s < .25 ? "red" : s < .75 ? "yellow" : "green")}]{s:N3}[/]";
    }

    private static WordSet[] RunSearch(Options options)
    {
        WordSet[] scoredSets = null!;

        AnsiConsole
           .Progress()
           .Columns(new SpinnerColumn(), new TaskDescriptionColumn(), new ProgressBarColumn(), new ElapsedTimeColumn())
           .Start(ctx =>
                  {
                      var candidateTask = ctx.AddTask("Creating candidates", maxValue: 1);
                      candidateTask.StartTask();

                      var candidates = CandidateSearcher.GenerateCandidates(options, p => candidateTask.Value(p));

                      candidateTask.Description($"Found [Aqua]{candidates.Length:N0}[/] candidates.");
                      candidateTask.Value(1);
                      candidateTask.StopTask();

                      if (candidates.Length is 0)
                      {
                          scoredSets = [];
                          return;
                      }
                      var candidatesChecking = CandidateScorer.SelectCandidatesToScore(candidates, options);
                      var scoringTask        = ctx.AddTask($"Performing full scoring on [green]{candidatesChecking.Length:N0}[/] candidates", maxValue: 1);

                      scoredSets = CandidateScorer.ScoreCandidates(candidatesChecking, options, p => scoringTask.Value(p));

                      scoringTask.Value(1);
                      scoringTask.StopTask();
                  });

        return scoredSets;
    }
}