using TMPro;
using UnityEngine;

public class GameEndUIManager : MonoBehaviour
{
    [Header("Result Text")]
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private TMP_Text elapsedTimeText;

    [Header("Debug Settings")]
    [SerializeField] private bool useDebugResults = true;
    [SerializeField] private string debugWinner = "PLAYER WINS!";
    [SerializeField] private float debugElapsedTime = 125f;

    private void Start()
    {
        DisplayResults();
    }

    private void DisplayResults()
    {
        string winner;
        float elapsedTime;

        if (useDebugResults)
        {
            winner = debugWinner;
            elapsedTime = debugElapsedTime;
        }
        else
        {
            winner = SessionResults.Winner;
            elapsedTime = SessionResults.ElapsedTime;
        }

        if (winnerText != null)
            winnerText.text = winner;

        if (elapsedTimeText != null)
        {
            int totalSeconds = Mathf.FloorToInt(elapsedTime);

            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            elapsedTimeText.text =
                $"{minutes:00}:{seconds:00}";
        }
    }
}
