
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class BuffNerfManager : MonoBehaviour
{
    private const int TokensPerWave = 5;

    [Header("Round Reference")]
    [SerializeField] private RoundManager roundManager;

    [Header("Token Configuration")]
    [SerializeField] private BuffNerfTokenData[] availableTokens;

    [Header("Arena Spawn Area")]
    [SerializeField] private BoxCollider spawnArea;

    [SerializeField] private Transform spawnedTokenParent;

    [Header("NavMesh Spawn Settings")]
    [SerializeField, Min(0.1f)]
    private float navMeshSampleRadius = 1.5f;

    [SerializeField, Min(0f)]
    private float spawnHeightAboveSurface = 0.8f;

    [SerializeField, Min(0f)]
    private float minimumSpawnSeparation = 3f;

    [SerializeField, Min(1)]
    private int attemptsPerToken = 50;

    [Header("Spawn Settings")]
    [SerializeField, Min(1f)]
    private float spawnInterval = 20f;

    private readonly List<GameObject> spawnedTokens =
        new List<GameObject>();

    private float spawnTimer;
    private bool isSpawning;

    private void Awake()
    {
        if (roundManager == null)
        {
            roundManager = FindFirstObjectByType<RoundManager>();
        }
    }

    private void OnEnable()
    {
        if (roundManager == null)
            return;

        roundManager.RoundStarted += OnRoundStarted;
        roundManager.RoundEnded += OnRoundEnded;
    }

    private void Start()
    {
        if (roundManager == null)
        {
            Debug.LogError(
                "BUFF NERF MANAGER | RoundManager reference is missing.",
                this
            );

            enabled = false;
            return;
        }

        if (!ValidateConfiguration())
        {
            enabled = false;
            return;
        }

        // Handles the case where this manager becomes active
        // after a round has already started.
        if (roundManager.IsRoundActive)
        {
            StartSpawning();
        }
    }

    private void Update()
    {
        if (!isSpawning)
            return;

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnNewWave();
        }
    }

    private bool ValidateConfiguration()
    {
        if (availableTokens == null ||
            availableTokens.Length < TokensPerWave)
        {
            Debug.LogError(
                "BUFF NERF MANAGER | At least five token data assets are required.",
                this
            );

            return false;
        }

        if (spawnArea == null)
        {
            Debug.LogError(
                "BUFF NERF MANAGER | Spawn Area is missing.",
                this
            );

            return false;
        }

        if (!spawnArea.enabled || spawnArea.bounds.size == Vector3.zero)
        {
            Debug.LogError(
                "BUFF NERF MANAGER | Spawn Area collider is disabled or invalid.",
                this
            );

            return false;
    }

        HashSet<BuffNerfTokenId> uniqueIds =
            new HashSet<BuffNerfTokenId>();

        foreach (BuffNerfTokenData data in availableTokens)
        {
            if (data == null)
            {
                Debug.LogError(
                    "BUFF NERF MANAGER | Token list contains an empty entry.",
                    this
                );

                return false;
            }

            if (!uniqueIds.Add(data.TokenId))
            {
                Debug.LogError(
                    $"BUFF NERF MANAGER | Duplicate token ID: {data.TokenId}.",
                    this
                );

                return false;
            }

            if (data.WorldPrefab == null)
            {
                Debug.LogError(
                    $"BUFF NERF MANAGER | World Prefab missing for {data.TokenId}.",
                    this
                );

                return false;
            }

            if (data.WorldPrefab.GetComponent<BuffNerfToken>() == null)
            {
                Debug.LogError(
                    $"BUFF NERF MANAGER | {data.WorldPrefab.name} " +
                    "does not contain BuffNerfToken.",
                    this
                );

                return false;
            }
        }

        return true;
    }

    private void OnRoundStarted(int round, float timeRemaining)
    {
        StartSpawning();
    }

    private void OnRoundEnded(
        int round,
        CharacterRoleType winner,
        int playerScore,
        int aiScore)
    {
        StopSpawning();
    }

    private void StartSpawning()
    {
        if (isSpawning)
            return;

        isSpawning = true;

        // Wait 20 seconds before spawning the first wave.
        spawnTimer = spawnInterval;

        Debug.Log(
            $"BUFF NERF MANAGER | Token spawning started. " +
            $"First wave in {spawnInterval:F1}s."
        );
    }

    private void StopSpawning()
    {
        isSpawning = false;
        spawnTimer = 0f;

        ClearCurrentWave();

        Debug.Log("BUFF NERF MANAGER | Token spawning stopped.");
    }

    private void SpawnNewWave()
    {
        // Remove uncollected tokens from the previous wave.
        ClearCurrentWave();

        // Find five valid and separated NavMesh positions.
        if (!TryGetRandomNavMeshPositions(out List<Vector3> positions))
        {
            Debug.LogWarning(
                "BUFF NERF MANAGER | Could not find five valid " +
                "NavMesh spawn positions. Retrying in 20 seconds.",
                this
            );

            spawnTimer = spawnInterval;
            return;
        }

        List<BuffNerfTokenData> selectedTokens =
            GetRandomUniqueTokens();

        for (int i = 0; i < TokensPerWave; i++)
        {
            SpawnToken(selectedTokens[i], positions[i]);
        }

        spawnTimer = spawnInterval;

        Debug.Log(
            $"BUFF NERF WAVE SPAWNED | " +
            $"TokenCount={spawnedTokens.Count} | " +
            $"NextWaveIn={spawnInterval:F1}s"
        );
    }

    private bool TryGetRandomNavMeshPositions(
        out List<Vector3> positions)
    {
        positions = new List<Vector3>();

        Bounds bounds = spawnArea.bounds;

        for (int tokenIndex = 0;
            tokenIndex < TokensPerWave;
            tokenIndex++)
        {
            bool foundPosition = false;

            for (int attempt = 0;
                attempt < attemptsPerToken;
                attempt++)
            {
                // Randomize X, Y, and Z.
                // Random Y allows upper platforms to be sampled too.
                Vector3 randomPoint = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y),
                    Random.Range(bounds.min.z, bounds.max.z)
                );

                if (!NavMesh.SamplePosition(
                    randomPoint,
                    out NavMeshHit hit,
                    navMeshSampleRadius,
                    NavMesh.AllAreas))
                {
                    continue;
                }

                // Ensure the sampled surface is inside our arena bounds.
                if (!bounds.Contains(hit.position))
                    continue;

                // Check that this position is not too close
                // to another token in the same wave.
                bool tooClose = false;

                foreach (Vector3 existingPosition in positions)
                {
                    if (Vector3.Distance(
                        hit.position,
                        existingPosition) < minimumSpawnSeparation)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                    continue;

                positions.Add(hit.position);
                foundPosition = true;
                break;
            }

            if (!foundPosition)
            {
                positions.Clear();
                return false;
            }
        }

        return positions.Count == TokensPerWave;
    }

    private List<BuffNerfTokenData> GetRandomUniqueTokens()
    {
        List<BuffNerfTokenData> pool =
            new List<BuffNerfTokenData>(availableTokens);

        Shuffle(pool);

        return pool.GetRange(0, TokensPerWave);
    }

    private void SpawnToken(
        BuffNerfTokenData data,
        Vector3 surfacePosition)
    {
        Vector3 spawnPosition =
            surfacePosition +
            Vector3.up * spawnHeightAboveSurface;

        Quaternion spawnRotation = Quaternion.Euler(
            0f,
            Random.Range(0f, 360f),
            0f
        );

        GameObject tokenObject = Instantiate(
            data.WorldPrefab,
            spawnPosition,
            spawnRotation,
            spawnedTokenParent
        );

        BuffNerfToken token =
            tokenObject.GetComponent<BuffNerfToken>();

        if (token == null)
        {
            Debug.LogError(
                $"BUFF NERF MANAGER | {tokenObject.name} " +
                "has no BuffNerfToken component.",
                tokenObject
            );

            Destroy(tokenObject);
            return;
        }

        token.Initialize(data);

        spawnedTokens.Add(tokenObject);
    }

    private void ClearCurrentWave()
    {
        foreach (GameObject token in spawnedTokens)
        {
            if (token != null)
            {
                Destroy(token);
            }
        }

        spawnedTokens.Clear();
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            T temporary = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temporary;
        }
    }

    private void OnDisable()
    {
        if (roundManager != null)
        {
            roundManager.RoundStarted -= OnRoundStarted;
            roundManager.RoundEnded -= OnRoundEnded;
        }

        StopSpawning();
    }
}
