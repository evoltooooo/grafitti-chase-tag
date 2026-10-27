
public static class SessionResults
{
    public static int PlayerScore { get; private set; }
    public static int AIScore { get; private set; }
    public static float ElapsedTime { get; private set; }
    public static string Winner { get; private set; }

    public static void SetResults(
        int playerScore,
        int aiScore,
        float elapsedTime)
    {
        PlayerScore = playerScore;
        AIScore = aiScore;
        ElapsedTime = elapsedTime;

        if (playerScore > aiScore)
            Winner = "PLAYER WINS!";
        else if (aiScore > playerScore)
            Winner = "AI WINS!";
        else
            Winner = "DRAW!";
    }

    public static void Reset()
    {
        PlayerScore = 0;
        AIScore = 0;
        ElapsedTime = 0f;
        Winner = string.Empty;
    }
}
