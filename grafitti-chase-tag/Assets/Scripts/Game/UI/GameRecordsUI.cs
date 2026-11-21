using TMPro;
using UnityEngine;

public class GameRecordsUI : MonoBehaviour
{
    [Header("Record Text References")]
    [SerializeField] private TMP_Text fastestChaserScoreText;
    [SerializeField] private TMP_Text longestEvaderScoreText;

    private void Start()
    {
        RefreshRecords();
    }

    public void RefreshRecords()
    {
        UpdateFastestChaserDisplay();
        UpdateLongestEvaderDisplay();
    }

    private void UpdateFastestChaserDisplay()
    {
        float fastestTime = GameRecordsManager.FastestChaserTime;

        if (fastestTime <= 0f)
        {
            fastestChaserScoreText.text = "--:--";
            return;
        }

        int minutes = Mathf.FloorToInt(fastestTime / 60f);
        int seconds = Mathf.FloorToInt(fastestTime % 60f);

        fastestChaserScoreText.text =
            $"{minutes:00}:{seconds:00}";
    }

    private void UpdateLongestEvaderDisplay()
    {
        int longestStreak = GameRecordsManager.LongestEvaderStreak;

        longestEvaderScoreText.text = $"{longestStreak} Rounds";
    }
}
