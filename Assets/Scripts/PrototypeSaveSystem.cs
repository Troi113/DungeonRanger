using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>Serializable snapshot of one inventory item.</summary>
[Serializable]
public sealed class PrototypeItemSave
{
    public bool Empty = true;
    public string ItemId;
    public int EquipmentSlot;
    public int UpgradeLevel;
    public bool IsCursed;
    public bool IsIdentified = true;
    public int Quantity = 1;
    public int Hits;

    public static PrototypeItemSave From(PrototypeInventoryItem item)
    {
        if (item == null) return new PrototypeItemSave();
        return new PrototypeItemSave
        {
            Empty = false,
            ItemId = item.ItemId,
            EquipmentSlot = (int)item.EquipmentSlot,
            UpgradeLevel = item.UpgradeLevel,
            IsCursed = item.IsCursed,
            IsIdentified = item.IsIdentified,
            Quantity = item.Quantity,
            Hits = item.Hits
        };
    }

    public PrototypeInventoryItem ToItem()
    {
        if (Empty || string.IsNullOrEmpty(ItemId)) return null;
        return new PrototypeInventoryItem(ItemId, (PrototypeEquipmentSlot)EquipmentSlot)
        {
            UpgradeLevel = UpgradeLevel,
            IsCursed = IsCursed,
            IsIdentified = IsIdentified,
            Hits = Hits,
            Quantity = Mathf.Max(1, Quantity)
        };
    }
}

/// <summary>An item lying on a floor that was not placed by generation (dropped, thrown, or from a chest).</summary>
[Serializable]
public sealed class PrototypeLooseItemSave
{
    public PrototypeItemSave Item;
    public float X;
    public float Z;
}

[Serializable]
public sealed class PrototypeEnemySave
{
    public bool Alive;
    public int Health;
    public float X;
    public float Z;
    public bool Awake;
}

/// <summary>Differences from a floor's freshly generated state.</summary>
[Serializable]
public sealed class PrototypeFloorSave
{
    public int Floor;
    public List<int> TakenGeneratedPickups = new();
    public List<PrototypeLooseItemSave> LooseItems = new();
    public List<PrototypeEnemySave> Enemies = new();
    public List<int> ActivatedTraps = new();
    public List<int> RevealedTraps = new();
    public List<int> BurntDoors = new();
    public bool ChestOpened;
    public List<int> VisitedCells = new();
    public List<int> MappedCells = new();
    public bool AmuletPresent;
    public float AmuletX;
    public float AmuletZ;
}

/// <summary>A whole run in progress. Floors are regenerated from the seed, then their saved differences are applied.</summary>
[Serializable]
public sealed class PrototypeSaveData
{
    public int Version = 1;
    public string LastPlayedUtc;
    public int RunSeed;
    public int Floor;
    public float PlayerX;
    public float PlayerZ;
    public int Health;
    public int Strength;
    public int Level;
    public int Experience;
    public int MaxHealth;
    public List<string> KnownPotions = new();
    public List<int> TalentRanks = new();
    public float HungerMovingTime;
    public int Score;
    public float ElapsedSeconds;
    public int DeepestFloor;
    public List<PrototypeItemSave> Inventory = new();
    public List<string> KnownScrolls = new();
    public List<PrototypeFloorSave> Floors = new();

    public DateTime LastPlayedLocal =>
        DateTime.TryParse(LastPlayedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time)
            ? time.ToLocalTime()
            : DateTime.MinValue;
}

/// <summary>
/// Reads and writes the single game-in-progress slot as JSON in Application.persistentDataPath.
/// Kept separate from gameplay; PrototypeDungeonRun builds and applies the data.
/// </summary>
public static class PrototypeSaveSystem
{
    private const string FileName = "game_in_progress.json";
    private static PrototypeSaveData pendingContinue;

    private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSave => File.Exists(SavePath);

    public static void Save(PrototypeSaveData data)
    {
        data.LastPlayedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data));
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not save the game in progress: {exception.Message}");
        }
    }

    public static PrototypeSaveData Load()
    {
        if (!HasSave) return null;
        try
        {
            return JsonUtility.FromJson<PrototypeSaveData>(File.ReadAllText(SavePath));
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not read the game in progress: {exception.Message}");
            return null;
        }
    }

    public static void Delete()
    {
        try
        {
            if (HasSave) File.Delete(SavePath);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not delete the game in progress: {exception.Message}");
        }
    }

    /// <summary>Main menu sets this before loading the gameplay scene; the run consumes it once.</summary>
    public static void RequestContinue(PrototypeSaveData data) => pendingContinue = data;

    public static PrototypeSaveData ConsumeContinue()
    {
        var data = pendingContinue;
        pendingContinue = null;
        return data;
    }
}
