
using TMPro;
using UnityEngine;

public class GameEndUIManager : MonoBehaviour
{
    [Header("Result Text")]
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private TMP_Text elapsedTimeText;

    private void Start()
    {
        DisplayResults();
    }

    private void DisplayResults()
    {
        if (winnerText != null)
            winnerText.text = SessionResults.Winner;

        if (elapsedTimeText != null)
        {
            int totalSeconds = Mathf.FloorToInt(
                SessionResults.ElapsedTime
            );

            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            elapsedTimeText.text =
                $"SESSION TIME  {minutes:00}:{seconds:00}";
        }
    }
}
