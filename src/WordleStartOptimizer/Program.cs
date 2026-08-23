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

        AnsiConsole.MarkupLine(SetMarkupBuilder.FormatWordSetMarkup(options.Set));

        Dictionary<int, double> greenChances  = new () { {0, 0}, {1, 0}, {2, 0}, {3, 0}, {4, 0}, {5, 0}, };
        Dictionary<int, double> yellowChances = new () { {0, 0}, {1, 0}, {2, 0}, {3, 0}, {4, 0}, {5, 0}, };

        for (int i = 0; i < Data.ValidGuesses.Length; i++)
        {
            if(!Data.WordIsValidAnswer[i])
                continue;

            int greens = 0, yellows = 0;

            foreach (short guessIndex in options.Set.WordIndexes)
            {
                var pattern = Data.PatternMatrix[guessIndex, i];

                int[] colors =
                    [
                        pattern % 3,
                        pattern / 3 % 3,
                        pattern / 9 % 3,
                        pattern / 27 % 3,
                        pattern / 81 % 3,
                    ];

                foreach (int color in colors)
                {
                    if (color is 1)
                        yellows++;
                    else if (color is 2)
                        greens++;
                }
            }
            greenChances[greens]   += 1D / Data.ValidAnswers.Length;
            yellowChances[yellows] += 1D / Data.ValidAnswers.Length;
        }

        Color[] colorArr = [Color.DarkRed, Color.OrangeRed1, Color.Orange1, Color.Yellow, Color.Green, Color.Lime];

        var greenChart = new BreakdownChart();
        greenChart.ValueFormatter = (d, _) => $"{d:P1}";
        for (int i = 0; i < 6; i++)
        {
            greenChart.AddItem($"{i}", greenChances[i], colorArr[i]);
        }

        var yellowChart = new BreakdownChart();
        yellowChart.ValueFormatter = (d, _) => $"{d:P1}";
        for (int i = 0; i < 6; i++)
        {
            yellowChart.AddItem($"{i}", yellowChances[i], colorArr[i]);
        }

        var grid = new Grid();

        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow(
            SetMarkupBuilder.BuildRawDataTable(options.Set),
            new Grid().AddColumn()
                      .AddRow(new Panel(greenChart).Header("Green Chances"))
                      .AddRow(new Panel(yellowChart).Header("Yellow Chances"))
                      .AddRow(new Panel(String.Join(", ", options.Set.WordIndexes.Where(x => Data.WordIsValidAnswer[x]).Select(x => Data.ValidGuesses[x]))).Header("Valid answers").Expand()));

        AnsiConsole.Write(grid);

        return 0;
    }
}