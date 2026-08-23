using CommandLine;

namespace WordleStartOptimizer.Models.Options;

[Verb("evaluate", isDefault: false, aliases: ["eval"], HelpText = "Evaluate the metrics of a given set.")]
public class EvaluationOptions
{
    [Value(0, MetaName = "Set", HelpText = "The word set to evaluate.", Required = true)]
    public IEnumerable<string> SetStrings
    {
        get;
        init
        {
            field = value;

            var     words   = value.ToArray();
            short[] indexes = new short[words.Length];

            for (int i = 0; i < words.Length; i++)
            {
                var word  = words[i];
                var index = Data.ValidGuesses.IndexOf(word);

                if (index is -1)
                    throw new ArgumentException($"{word} is not a valid wordle guess and cannot be marked as required.", nameof(Set));

                indexes[i] = (short)index;
            }

            Set = new WordSet(indexes);
        }
    } = default!;

    public WordSet Set { get; init; } = default!;
}