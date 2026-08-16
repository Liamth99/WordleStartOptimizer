namespace WordleStartOptimizer.Tests.WordMaskTests;

public class PatternMatchingTests
{
    [Theory]
    [InlineData("*****", "crane")]
    [InlineData("c****", "crane")]
    [InlineData("ch***", "crane", "think")]
    public void ShouldMatchPattern(string patternString, params string[] words)
    {
        var pattern = WordMask.FromMaskPattern(patternString);
        var masks   = words.Select(x => new WordMask(x));

        pattern.AllCombinedMatchesPattern(masks).ShouldBeTrue();
    }

    [Theory]
    [InlineData("zzzzz", "crane")]
    [InlineData("think", "crane")]
    [InlineData("zzzzz", "crane", "think")]
    public void ShouldNotMatchPattern(string patternString, params string[] words)
    {
        var pattern = WordMask.FromMaskPattern(patternString);
        var masks   = words.Select(x => new WordMask(x));

        pattern.AllCombinedMatchesPattern(masks).ShouldBeFalse();
    }
}