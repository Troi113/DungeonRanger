using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Persistent high scores: the top runs (death or victory) saved as JSON in
/// Application.persistentDataPath. Kept separate from gameplay; the run reports results here.
/// </summary>
public static class PrototypeLeaderboard
{
    [Serializable]
    public sealed class Entry
    {
        public int Score;
        public int Floor;
        public float Seconds;
        public bool Victory;
        public string DateUtc;

        public string DateText => DateTime.TryParse(DateUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d)
            ? d.ToLocalTime().ToString("MMM d, yyyy") : "";
    }

    [Serializable]
    private sealed class Table
    {
        public List<Entry> Entries = new();
    }

    private const string FileName = "leaderboard.json";
    private const int MaxEntries = 10;
    private static Table cache;

    /// <summary>The most recently recorded run (shown on the game-over and victory screens).</summary>
    public static Entry LastRun { get; private set; }
    public static bool LastRunWasBest { get; private set; }

    private static string PathOnDisk => Path.Combine(Application.persistentDataPath, FileName);

    public static IReadOnlyList<Entry> Entries => Load().Entries;
    public static int BestScore => Load().Entries.Count > 0 ? Load().Entries[0].Score : 0;

    public static void Record(int score, int floor, float seconds, bool victory)
    {
        var table = Load();
        var previousBest = BestScore;
        LastRun = new Entry
        {
            Score = score,
            Floor = floor,
            Seconds = seconds,
            Victory = victory,
            DateUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
        };
        LastRunWasBest = score > previousBest;
        table.Entries.Add(LastRun);
        table.Entries.Sort((a, b) => b.Score.CompareTo(a.Score));
        if (table.Entries.Count > MaxEntries) table.Entries.RemoveRange(MaxEntries, table.Entries.Count - MaxEntries);
        try
        {
            File.WriteAllText(PathOnDisk, JsonUtility.ToJson(table));
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not save the leaderboard: {exception.Message}");
        }
    }

    private static Table Load()
    {
        if (cache != null) return cache;
        cache = new Table();
        try
        {
            if (File.Exists(PathOnDisk)) cache = JsonUtility.FromJson<Table>(File.ReadAllText(PathOnDisk)) ?? new Table();
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not read the leaderboard: {exception.Message}");
        }
        cache.Entries ??= new List<Entry>();
        return cache;
    }

    public static string FormatTime(float seconds)
    {
        var total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{total / 60:00}:{total % 60:00}";
    }
}
