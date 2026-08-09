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
            int bit = 1 << (c - 'a' + 1);

            // duplicate letter in the same word, make negative to mark as to be ignored later
            if ((mask & bit) is not 0)
            {
                mask |= bit;

                if (mask > 0)
                    mask = -mask;
            }
            else
                mask |= bit;
        }

        return mask;
    }
}