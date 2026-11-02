using UnityEngine;
using TMPro;

public class BuffNerfMessageUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BuffNerfManager buffNerfManager;
    [SerializeField] private TMP_Text messageText;

    [Header("Countdown Settings")]
    [SerializeField, Min(1)]
    private int warningThreshold = 5;

    [SerializeField]
    private Color normalCountdownColor = Color.white;

    [SerializeField]
    private Color warningCountdownColor = Color.yellow;

    [Header("Spawned Message Settings")]
    [SerializeField]
    private string spawnedMessage = "TOKENS SPAWNED!";

    [SerializeField]
    private Color spawnedMessageColor = Color.green;

    [SerializeField, Min(0.1f)]
    private float spawnedMessageDuration = 2f;

    private bool showingSpawnedMessage;
    private float spawnedMessageTimer;

    private void Awake()
    {
        if (buffNerfManager == null)
        {
            buffNerfManager =
                FindFirstObjectByType<BuffNerfManager>();
        }

        if (messageText == null)
        {
            Debug.LogError(
                "BUFF NERF MESSAGE UI | Message Text is missing.",
                this
            );
        }

        if (buffNerfManager == null)
        {
            Debug.LogError(
                "BUFF NERF MESSAGE UI | BuffNerfManager is missing.",
                this
            );
        }
    }

    private void OnEnable()
    {
        if (buffNerfManager != null)
        {
            buffNerfManager.TokensSpawned += OnTokensSpawned;
        }
    }

    private void OnDisable()
    {
        if (buffNerfManager != null)
        {
            buffNerfManager.TokensSpawned -= OnTokensSpawned;
        }
    }

    private void Update()
    {
        if (buffNerfManager == null || messageText == null)
            return;

        if (!buffNerfManager.IsSpawning)
        {
            messageText.text = "";
            showingSpawnedMessage = false;
            return;
        }

        // Show the spawned notification temporarily.
        if (showingSpawnedMessage)
        {
            spawnedMessageTimer -= Time.deltaTime;

            if (spawnedMessageTimer > 0f)
            {
                messageText.text = spawnedMessage;
                messageText.color = spawnedMessageColor;
                return;
            }

            showingSpawnedMessage = false;
        }

        // Read the actual remaining time from the manager.
        int secondsRemaining = Mathf.CeilToInt(
            buffNerfManager.TimeUntilNextWave
        );

        if (secondsRemaining <= 0)
        {
            messageText.text = "";
            return;
        }

        messageText.text =
            $"Next tokens spawn in {secondsRemaining} seconds";

        // Change color during the final countdown.
        messageText.color =
            secondsRemaining <= warningThreshold
                ? warningCountdownColor
                : normalCountdownColor;
    }

    private void OnTokensSpawned()
    {
        showingSpawnedMessage = true;
        spawnedMessageTimer = spawnedMessageDuration;

        if (messageText != null)
        {
            messageText.text = spawnedMessage;
            messageText.color = spawnedMessageColor;
        }
    }
}