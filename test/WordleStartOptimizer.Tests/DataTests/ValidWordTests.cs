namespace WordleStartOptimizer.Tests.DataTests;

public class ValidWordTests
{
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
}