using CommandLine;
using CommandLine.Text;

namespace WordleStartOptimizer.Models.Options;

[Verb("solve", isDefault: false, HelpText = "Generate optimized starting word sets.")]
public class SolveOptions
{
    [Value(0, MetaName = "GuessResults", HelpText = "The word set to evaluate.", Required = true)]
    public IEnumerable<string> GuessResultsStrings
    {
        get;
        set
        {
            field = value;

            var resultsStrings = value.Select(x => x.Split("|", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToArray();
            Results = new GuessResult[resultsStrings.Length];

            for (int i = 0; i < resultsStrings.Length; i++)
            {
                string[] resultsString = resultsStrings[i];
                if (resultsString.Length is not 2)
                    throw new ArgumentException("Expected format for Guess Result is `guess|pattern`");

                var guess         = resultsString[0];
                var guessIndex    = (short)Data.ValidGuesses.IndexOf(guess);

                if (guessIndex is -1)
                    throw new ArgumentException($"{guess} is not a valid wordle guess.");

                var patternString = resultsString[1];

                if (patternString.Length is not 5 || patternString.Any(x => x is not '_' and not 'g' and not 'y'))
                    throw new ArgumentException("Expected format for a Guess Results pattern is `_g_y_` where _ is grey");

                byte pattern = 0;

                for (int j = 0; j < 5; j++)
                {
                    char c = patternString[j];
                    if (c is 'y')
                        pattern += (byte)Math.Pow(3, j);
                    else if (c is 'g')
                        pattern += (byte)(2 * Math.Pow(3, j));
                }

                Results[i] = new GuessResult(guessIndex, pattern);
            }
        }
    } = default!;

    public GuessResult[] Results { get; set; } = default!;

    public List<short> GetValidGuesses()
    {
        var results = new List<short>();
        for (short i = 0; i < Data.ValidGuesses.Length; i++)
        {
            var valid = true;
            foreach (var guessResult in Results)
            {
                if (Data.PatternMatrix[guessResult.GuessIndex, i] != guessResult.Pattern)
                {
                    valid = false;
                    break;
                }
            }

            if(valid)
                results.Add(i);
        }

        return results;
    }

    [Usage]
    public static IEnumerable<Example> Examples =>
    [
        new Example(
            "Determine the next best guess for a game of wordle after guessing tares and colin",
            new SolveOptions() { GuessResultsStrings = ["tares=_____", "colin=_y_yg"]})
    ];
}

public class GuessResult(short guessIndex, byte pattern)
{
    public short  GuessIndex { get; init; } = guessIndex;
    public byte   Pattern    { get; init; } = pattern;
}