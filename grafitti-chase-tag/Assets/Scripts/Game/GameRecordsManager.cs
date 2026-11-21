using System;
using System.IO;
using UnityEngine;

[Serializable]
public class GameRecordsData
{
    public float fastestChaserTime;
    public int longestEvaderStreak;
}

public static class GameRecordsManager
{
    private static GameRecordsData records;
    private static int currentEvaderStreak;
    private static bool isLoaded;

    private static string SavePath =>
        Path.Combine(
            Application.persistentDataPath,
            "game_records.json"
        );
    
    public static string GetSavePath()
    {
        return SavePath;
    }

    public static float FastestChaserTime
    {
        get
        {
            LoadRecords();
            return records.fastestChaserTime;
        }
    }

    public static int LongestEvaderStreak
    {
        get
        {
            LoadRecords();
            return records.longestEvaderStreak;
        }
    }

    public static int CurrentEvaderStreak =>
        currentEvaderStreak;

    public static void StartNewSession()
    {
        LoadRecords();

        currentEvaderStreak = 0;
    }

    public static void RegisterRoundResult(
        CharacterRoleType playerRole,
        CharacterRoleType winningRole,
        float elapsedTime)
    {
        LoadRecords();

        // Player is Chaser.
        // Only a successful Player Chaser victory
        // can update the fastest record.
        if (playerRole == CharacterRoleType.Chaser)
        {
            if (winningRole != CharacterRoleType.Chaser)
                return;

            if (elapsedTime <= 0f ||
                float.IsNaN(elapsedTime) ||
                float.IsInfinity(elapsedTime))
                return;

            if (records.fastestChaserTime <= 0f ||
                elapsedTime < records.fastestChaserTime)
            {
                records.fastestChaserTime = elapsedTime;
                SaveRecords();
            }

            // Do not modify the Evader streak.
            return;
        }

        // Player is Evader.
        if (playerRole == CharacterRoleType.Evader)
        {
            if (winningRole == CharacterRoleType.Evader)
            {
                currentEvaderStreak++;

                if (currentEvaderStreak >
                    records.longestEvaderStreak)
                {
                    records.longestEvaderStreak =
                        currentEvaderStreak;

                    SaveRecords();
                }
            }
            else
            {
                // Only an Evader loss resets the streak.
                currentEvaderStreak = 0;
            }
        }
    }

    private static void LoadRecords()
    {
        if (isLoaded)
            return;

        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);

                records =
                    JsonUtility.FromJson<GameRecordsData>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"GAME RECORDS | Failed to load records: {exception.Message}"
                );
            }
        }

        if (records == null)
            records = new GameRecordsData();

        if (records.fastestChaserTime < 0f ||
            float.IsNaN(records.fastestChaserTime) ||
            float.IsInfinity(records.fastestChaserTime))
        {
            records.fastestChaserTime = 0f;
        }

        if (records.longestEvaderStreak < 0)
            records.longestEvaderStreak = 0;

        isLoaded = true;
    }

    private static void SaveRecords()
    {
        try
        {
            string json =
                JsonUtility.ToJson(records, true);

            File.WriteAllText(SavePath, json);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"GAME RECORDS | Failed to save records: {exception.Message}"
            );
        }
    }
}
