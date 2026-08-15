using System.Diagnostics;

namespace WordleStartOptimizer.Models;

[DebuggerDisplay("{Word}")]
public readonly struct WordMask : IEqualityComparer<WordMask>, IEquatable<WordMask>
{
    /// <summary>
    /// Represents a compact bitmask used to encode character and positional data
    /// for words in a space-efficient manner.
    /// </summary>
    public uint Mask { get; }

    /// <summary>
    /// Encodes a set of unique letters from a word into a compact bitmask representation
    /// where each bit corresponds to the presence (or absence) of a letter.
    /// </summary>
    /// <remarks>Negative masks have duplicate letters.</remarks>
    public int LetterMask { get; }

    public string Word => ToString();

    public override string ToString() => string.Join("", Chars);

    public char[] Chars => [this[0], this[1], this[2], this[3], this[4],];

    public bool ContainsDuplicateLetters => LetterMask < 0;

    public bool HasCharacter(char c) => (LetterMask & (1 << (c - 'a'))) is not 0;

    public char this[int index] => (char)('a' + ((Mask >> (index * 5)) & 0x1F) - 1);

    public WordMask(uint mask, int letterMask)
    {
        Mask       = mask;
        LetterMask = letterMask;
    }

    public WordMask(string word)
    {
        Mask       = 0;
        LetterMask = 0;
        for (int i = 0; i < 5; i++)
        {
            char c = word[i];
            Mask       |= (uint)(((c - 'a' + 1) & 0x1FU) << (i * 5));

            int letterBit = 1 << (c - 'a');

            if ((LetterMask & letterBit) is not 0) // duplicate letter in the same word, make negative to mark as to be ignored later
            {
                LetterMask |= letterBit;

                if (LetterMask > 0)
                    LetterMask = -LetterMask;
            }
            else
                LetterMask |= letterBit;
        }
    }

    public static WordMask FromMaskPattern(string pattern)
    {
        uint mask       = 0;
        int  letterMask = 0;
        for (int i = 0; i < 5; i++)
        {
            char c = pattern[i];

            if(c is '*')
                continue;

            mask |= (uint)(((c - 'a' + 1) & 0x1F) << (i * 5));

            int letterBit = 1 << (c - 'a');

            if ((letterMask & letterBit) is not 0) // duplicate letter in the same word, make negative to mark as to be ignored later
            {
                letterMask |= letterBit;

                if (letterMask > 0)
                    letterBit = -letterBit;
            }

            letterMask |= letterBit;
        }

        return new WordMask(mask, letterMask);
    }

    public static WordMask operator &(WordMask left, WordMask right)
    {
        uint mask       = 0;
        int  letterMask = 0;
        for (int i = 0; i < 5; i++)
        {
            var leftVal  = (left.Mask >> (i * 5)) & 0x1F;
            var rightVal = (right.Mask >> (i * 5)) & 0x1F;

            if(leftVal == rightVal)
                mask |= leftVal << (i * 5);
            else
                continue;

            int letterBit = 1 << (left[i] - 'a');

            if ((letterMask & letterBit) is not 0) // duplicate letter in the same word, make negative to mark as to be ignored later
            {
                letterMask |= letterBit;

                if (letterMask > 0)
                    letterBit = -letterBit;
            }

            letterMask |= letterBit;
        }

        return new WordMask(mask, letterMask);
    }

    public bool AllCombinedMatchesPattern(IEnumerable<WordMask> masks)
    {
        if (Mask is 0)
            return true;

        var chars = new char[] {'`', '`', '`', '`', '`',};
        foreach (WordMask mask in masks)
        {
            var currentMask = mask & this;

            for (int i = 0; i < 5; i++)
            {
                if(this[i] is '`')
                    continue;

                char c = currentMask.Chars[i];
                if (this[i] is not '`' && c is not '`')
                    chars[i] = c;
            }
        }

        return this == new WordMask(new string(chars));
    }

    public bool Equals(WordMask x, WordMask y)
    {
        return x.Mask == y.Mask;
    }

    public int GetHashCode(WordMask obj)
    {
        return (int)obj.Mask;
    }

    public bool Equals(WordMask other) => Mask == other.Mask;

    public override bool Equals(object? obj) => obj is WordMask other && Equals(other);

    public override int GetHashCode() => (int)Mask;

    public static bool operator ==(WordMask left, WordMask right) => left.Equals(right);

    public static bool operator !=(WordMask left, WordMask right) => !left.Equals(right);
}