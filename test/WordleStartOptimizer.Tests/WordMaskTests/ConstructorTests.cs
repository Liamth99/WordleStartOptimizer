namespace WordleStartOptimizer.Tests.WordMaskTests;

public class ConstructorTests
{
    [Theory]
    [InlineData("aaaaa", 0b00001_00001_00001_00001_00001U)]
    [InlineData("zzzzz", 0b11010_11010_11010_11010_11010U)]
    [InlineData("crane", 0b00101_01110_00001_10010_00011U)]
    [InlineData("stool", 0b01100_01111_01111_10100_10011U)]
    public void MaskCorrectly(string word, uint mask)
    {
        new WordMask(word).Mask.ShouldBe(mask);
    }

    [Theory]
    [InlineData("aaaaa", -1)]
    [InlineData("kkkkk", -1 << 10)]
    [InlineData("rrrrr", -1 << 17)]
    [InlineData("zzzzz", -1 << 25)]
    [InlineData("abcde", 0b11111)]
    [InlineData("crane", 0b100010000000010101)]
    public void LetterMaskCorrect(string word, int letterMask)
    {
        new WordMask(word).LetterMask.ShouldBe(letterMask);
    }
}