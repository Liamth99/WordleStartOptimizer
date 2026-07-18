using System.Diagnostics;

namespace WordleStartOptimizer.Models;

[DebuggerDisplay("{Word}")]
public readonly struct WordInfo
{
    public WordInfo(string word)
    {
        Word = word;
        Mask = GetMask(word);
    }

    public WordInfo(string word, int mask )
    {
        Word = word;
        Mask = mask;
    }

    public string Word { get; }
    public int    Mask { get; }

    public static int GetMask(string word)
    {
        int mask = 0;

        foreach (char c in word)
        {
            int bit = c - 'a' + 1;

            // duplicate letter in the same word, make negative to mark as to be ignored later
            if ((mask & (1 << bit)) is not 0)
                return -1;

            mask |= 1 << bit;
        }

        return mask;
    }
}