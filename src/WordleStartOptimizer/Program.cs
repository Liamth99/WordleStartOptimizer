using CommandLine;
using WordleStartOptimizer.Models;
using Spectre.Console;
using WordleStartOptimizer.Models.Options;
using WordleStartOptimizer.Output;
using WordleStartOptimizer.Search;

namespace WordleStartOptimizer;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        Data.Initialize(Environment.ProcessorCount);
        return await Parser.Default
                           .ParseArguments<SetGenerationOptions, EvaluationOptions>(args)
                           .MapResult(
                                (SetGenerationOptions o) => RunGenSetAsync(o),
                                (EvaluationOptions    o) => RunEvaluateSetAsync(o),
                                 _ => Task.FromResult(1)
                                );
    }

    private static async Task<int> RunGenSetAsync(SetGenerationOptions options)
    {
        await VersionChecker.CheckVersionAsync();
        Data.Initialize(options.ThreadCount);
        if (options.RequiredWordsIndexes?.Length >= options.SetSize)
        {
            AnsiConsole.WriteException(new ArgumentException($"Required words length must be less than Set Size ({options.SetSize}).", nameof(options.RequiredWords)));
            return 1;
        }

        AnsiConsole.MarkupLine($"Generating starting word sets with {options.SetSize} words.");
        AnsiConsole.MarkupLine($"Using [red]{options.ThreadCount}[/] threads.");
        if (options.RequiredWordsIndexes is not null)
        {
            AnsiConsole.MarkupLine($"Using required words: {string.Join(", ", options.RequiredWordsIndexes.Select(x => $"[cyan]{Data.ValidGuesses[x]}[/]"))}");
        }
        if (options.RequiredLetters is not null)
        {
            AnsiConsole.MarkupLine($"Using required letters: {string.Join(", ", options.RequiredLetters.Select(x => $"[cyan]{x}[/]"))}");
        }
        if (options.BlockedLetters is not null)
        {
            AnsiConsole.MarkupLine($"Excluding blocked letters: {string.Join(", ", options.BlockedLetters.Select(x => $"[red]{x}[/]"))}");
        }

        var scoredSets = RunSearch(options);

        if (scoredSets.Length is 0)
        {
            SetGenerationReporter.ReportNoResults();
            return 0;
        }

        if (scoredSets.Length is 1)
        {
            SetGenerationReporter.ReportSingleResult(scoredSets[0], options);
            return 0;
        }

        var scoringContext = new WordSetScoringContext(scoredSets);

        if (options.VerboseScoring)
        {
            SetGenerationReporter.ReportGlobalStats(scoringContext);
        }

        WordSet[] bestResults = scoredSets
                         .OrderByDescending(set => scoringContext.Score(set, options))
                         .Take(options.TopResults)
                         .ToArray();

        SetGenerationReporter.ReportTopResults(bestResults, scoringContext, options);

        return 0;
    }

    private static WordSet[] RunSearch(SetGenerationOptions options)
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

    private static async Task<int> RunEvaluateSetAsync(EvaluationOptions options)
    {
        await VersionChecker.CheckVersionAsync();

        AnsiConsole.Write(SetMarkupBuilder.BuildRawDataTable(options.Set));
        return 1;
    }
}