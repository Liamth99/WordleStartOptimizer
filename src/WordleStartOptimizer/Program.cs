using CommandLine;
using WordleStartOptimizer.Models;
using Spectre.Console;
using WordleStartOptimizer.Output;
using WordleStartOptimizer.Search;

namespace WordleStartOptimizer;

internal class Program
{
    public static Options Options { get; private set; } = null!;

    public static void Main(string[] args)
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

        var versionCheckTask = VersionChecker.CheckVersionAsync();

        Data.Initialize(Options.ThreadCount);
        Task.WaitAll(versionCheckTask);

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

        var scoredSets = RunSearch(Options);

        if (scoredSets.Length is 0)
        {
            SetGenerationReporter.ReportNoResults();
            return;
        }

        if (scoredSets.Length is 1)
        {
            SetGenerationReporter.ReportSingleResult(scoredSets[0], Options);
            return;
        }

        var scoringContext = new WordSetScoringContext(scoredSets);

        if (Options.VerboseScoring)
        {
            SetGenerationReporter.ReportGlobalStats(scoringContext);
        }

        WordSet[] bestResults = scoredSets
                         .OrderByDescending(set => scoringContext.Score(set, Options))
                         .Take(Options.TopResults)
                         .ToArray();

        SetGenerationReporter.ReportTopResults(bestResults, scoringContext, Options);
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