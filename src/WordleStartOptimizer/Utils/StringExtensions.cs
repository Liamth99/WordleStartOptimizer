namespace WordleStartOptimizer.Utils;

public static class StringExtensions
{
    private static byte[] _powersOf3 = [1, 3, 9, 27, 81, ];

    public static byte EncodePatternString(this string pattern)
    {
        if (pattern.Length is not 5)
            throw new ArgumentException($"{pattern} is not a valid pattern. Expected format for a Guess Results pattern is `_g_y_` where _ is grey.", nameof(pattern));

        byte code = 0;

        for (int i = 0; i < 5; i++)
        {
            code += pattern[i] switch
                {
                    'y' or 'Y' => _powersOf3[i],
                    'g' or 'G' => (byte)(2 * _powersOf3[i]),
                    _          => throw new ArgumentException($"{pattern} is not a valid pattern. Expected format for a Guess Results pattern is `_g_y_` where _ is grey.", nameof(pattern))
                };
        }

        return code;
    }

    public static byte CalculatePatternCode(string guess, string answer)
    {
        byte code       = 0;
        int  multiplier = 1;

        int[] remaining = new int[26];
        int[] states    = new int[5];

        for (int k = 0; k < 5; k++)
        {
            remaining[answer[k] - 'a']++;
        }

        // Calc greens
        for (int charI = 0; charI < 5; charI++)
        {
            char c = guess[charI];

            if (c == answer[charI])
            {
             states[charI] =  2;
             remaining[c - 'a']--;
            }
        }

        // Calc rest
        for (int charI = 0; charI < 5; charI++)
        {
            if(states[charI] is 2)
             continue;

            char c = guess[charI];

            if (remaining[c - 'a'] > 0)
            {
             states[charI] = 1;
             remaining[c - 'a']--;
            }
            else
            {
             states[charI] = 0;
            }
        }

        foreach (int state in states)
        {
         code       += (byte)(state * multiplier);
         multiplier *= 3;
        }

        return code;
    }
}