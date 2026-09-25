 using WordleStartOptimizer.Utils;

namespace WordleStartOptimizer.Tests.DataTests;

public class PatternCodeTests
{
    [Theory]
    [InlineData("crane", "crane", "ggggg")]
    [InlineData("crane", "slate", "__g_g")]
    [InlineData("eerie", "feeds", "yg___")]
    [InlineData("raise", "arise", "yyggg")]
    [InlineData("apple", "ample", "g_ggg")]
    [InlineData("eerie", "erase", "g_y_g")]
    [InlineData("adieu", "stoic", "__y__")]
    [InlineData("torch", "stoic", "yy_y_")]
    [InlineData("gymps", "stoic", "____y")]
    public void PatternMatrixCodesCorrect(string guess, string answer, string pattern)
    {
        var code = Data.PatternMatrix[Data.ValidGuesses.IndexOf(guess), Data.ValidGuesses.IndexOf(answer)];
        code.ShouldBe(pattern.EncodePatternString());
    }
}