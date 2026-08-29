using CommandLine;
using WordleStartOptimizer.Models;
using Spectre.Console;
using WordleStartOptimizer.Models.Options;
using WordleStartOptimizer.Models.Search;
using WordleStartOptimizer.Output;

namespace WordleStartOptimizer;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        Data.Initialize(Environment.ProcessorCount);
        return await Parser.Default
                           .ParseArguments<SetGenerationOptions, EvaluationOptions, SolveOptions>(args)
                           .MapResult(
                                (SetGenerationOptions o) => RunGenSetAsync(o),
                                (EvaluationOptions    o) => RunEvaluateSetAsync(o),
                                (SolveOptions         o) => RunSolveAsync(o),
                                _ => Task.FromResult(1)
                                );
    }

    private static async Task<int> RunGenSetAsync(SetGenerationOptions options)
    {
        await VersionChecker.CheckVersionAsync();

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

        int[,] greenCounts  = new int[options.Set.Count, 5];
        int[,] yellowCounts = new int[options.Set.Count, 5];
        var    validWords   = options.Set.WordIndexes.Where(x => Data.WordIsValidAnswer[x]).Select(x => Data.ValidGuesses[x]).ToArray();

        for (int i = 0; i < Data.ValidGuesses.Length; i++)
        {
            if(!Data.WordIsValidAnswer[i])
                continue;

            for (int j = 0; j < options.Set.WordIndexes.Length; j++)
            {
                short guessIndex = options.Set.WordIndexes[j];
                var   pattern    = Data.PatternMatrix[guessIndex, i];

                int[] colors =
                    [
                        pattern % 3,
                        pattern / 3 % 3,
                        pattern / 9 % 3,
                        pattern / 27 % 3,
                        pattern / 81 % 3,
                    ];

                for (int letterIndex = 0; letterIndex < 5; letterIndex++)
                {
                    int color = colors[letterIndex];

                    if (color is 1)
                        yellowCounts[j, letterIndex]++;
                    else if (color is 2)
                        greenCounts[j, letterIndex]++;
                }
            }
        }

        var grid = new Grid();

        grid.AddColumn();
        grid.AddColumn();

        var leftColumnContent = new Grid()
                               .AddColumn()
                               .AddRow(SetMarkupBuilder.BuildRawDataTable(options.Set));

        if (validWords.Length > 0)
            leftColumnContent.AddRow(new Panel(string.Join(", ", validWords.Select(x => $"[cyan]{x}[/]"))).Header("Valid answers").Expand());
        
        grid.AddRow(
            leftColumnContent,
            SetMarkupBuilder.BuildSetBreakDown(options.Set,  greenCounts, yellowCounts)
        );

        AnsiConsole.Write(grid);

        return 0;
    }

    private static async Task<int> RunSolveAsync(SolveOptions solveOptions)
    {
        await VersionChecker.CheckVersionAsync();

        var validIndexes = solveOptions.GetValidGuesses();
        List <(short wordIndex, int worstRemaining, double entropy)> results = [];

        foreach (short index in validIndexes)
        {
            Dictionary<byte, int> patternCounts = [];
            foreach (short validIndex in validIndexes)
            {
                var pattern = Data.PatternMatrix[index, validIndex];

                if (!patternCounts.TryAdd(pattern, 1))
                    patternCounts[pattern]++;
            }

            double entropy        = 0;
            int    worstRemaining = 0;

            foreach (int patternCount in patternCounts.Values)
            {
                var probability        = patternCount / (double)validIndexes.Count;
                entropy -= probability * Math.Log2(probability);

                if (worstRemaining < patternCount)
                    worstRemaining = patternCount;
            }

            results.Add(new (index, worstRemaining, entropy));
        }

        var table = new Table().AddColumns("Word", "Worst Case Remaining", "Entropy");

        foreach (var word in results.OrderByDescending(x => x.entropy).ThenByDescending(x => Data.WordIsValidAnswer[x.wordIndex]))
        {
            table.AddRow($"[cyan]{Data.ValidGuesses[word.wordIndex]}[/]", $"{word.worstRemaining:N0}", $"{word.entropy:N3}");
        }

        AnsiConsole.Write(table);

        return 0;
    }
}