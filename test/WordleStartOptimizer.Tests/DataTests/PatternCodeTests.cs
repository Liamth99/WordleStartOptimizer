namespace WordleStartOptimizer.Tests.DataTests;

public class PatternCodeTests
{
    public enum Color
    {
        Grey, Yellow, Green,
    }

    [Theory]
    [InlineData("crane", "crane", Color.Green,  Color.Green,  Color.Green,  Color.Green, Color.Green)]
    [InlineData("crane", "slate", Color.Grey,   Color.Grey,   Color.Green,  Color.Grey,  Color.Green)]
    [InlineData("eerie", "feeds", Color.Yellow, Color.Green,  Color.Grey,   Color.Grey,  Color.Grey)]
    [InlineData("raise", "arise", Color.Yellow, Color.Yellow, Color.Green,  Color.Green, Color.Green)]
    [InlineData("apple", "ample", Color.Green,  Color.Grey,   Color.Green,  Color.Green, Color.Green)]
    [InlineData("eerie", "erase", Color.Green,  Color.Grey,   Color.Yellow, Color.Grey,  Color.Green)]
    public void PatternMatrixCodesCorrect(string guess, string answer, params Color[] colors)
    {
        var code = Data.PatternMatrix[Data.ValidGuesses.IndexOf(guess), Data.ValidGuesses.IndexOf(answer)];
        DecodeColor(code).ShouldBe(colors);
    }

    [Fact]
    public void ValidAnswersCorrect()
    {
        int validWords = 0;

        for (int i = 0; i < Data.ValidGuesses.Length; i++)
        {
            if (Data.WordIsValidAnswer[i])
            {
                Data.ValidAnswers.ShouldContain(Data.ValidGuesses[i]);
                validWords++;
            }
        }

        validWords.ShouldBe(Data.ValidAnswers.Length);
    }

    private static Color[] DecodeColor(byte code)
    {
        return
        [
            (Color)(code % 3),
            (Color)(code / 3 % 3),
            (Color)(code / 9 % 3),
            (Color)(code / 27 % 3),
            (Color)(code / 81 % 3),
        ];
    }
}