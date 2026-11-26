using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Run-specific scroll labels and the six currently supported effects.</summary>
[RequireComponent(typeof(PrototypeInventory), typeof(PrototypePlayerMovement))]
public sealed class PrototypeScrolls : MonoBehaviour
{
    private static readonly string[] Ids =
    {
        "scroll_challenge", "scroll_upgrade", "scroll_remove_curse",
        "scroll_teleportation", "scroll_identify", "scroll_magic_mapping"
    };
    private static readonly string[] Names =
    {
        "Scroll of Challenge", "Scroll of Upgrade", "Scroll of Remove Curse",
        "Scroll of Teleportation", "Scroll of Identify", "Scroll of Magic Mapping"
    };
    private static readonly string[] Runes = { "Ash", "Moon", "Oak", "Star", "Sun", "Wave" };
    private readonly Dictionary<string, string> labels = new();
    private readonly HashSet<string> known = new();
    private System.Random random;
    private PrototypeInventory inventory;
    private PrototypePlayerMovement player;

    public static int Count => Ids.Length;
    public static string GetId(int index) => Ids[index];

    private void Awake()
    {
        inventory = GetComponent<PrototypeInventory>();
        player = GetComponent<PrototypePlayerMovement>();
        Initialize(PrototypeLevelGenerator.Seed);
    }

    public void Initialize(int seed)
    {
        random = new System.Random(seed);
        known.Clear();
        labels.Clear();
        var available = new List<string>(Runes);
        foreach (var id in Ids)
        {
            var index = random.Next(available.Count);
            labels.Add(id, available[index]);
            available.RemoveAt(index);
        }
    }

    public static bool IsScroll(string id) => Array.IndexOf(Ids, id) >= 0;
    public bool IsKnown(string id) => known.Contains(id);
    public static bool RequiresTarget(string id) => id == "scroll_identify" || id == "scroll_upgrade";

    public string Description(string id)
    {
        if (!IsKnown(id)) return "Its effect is unknown until it is identified or read.";
        return id switch
        {
            "scroll_challenge" => "Alerts all enemies on this floor to your location.",
            "scroll_upgrade" => "Choose a melee weapon to increase its damage and remove its curse.",
            "scroll_remove_curse" => "Removes curses from carried and equipped items.",
            "scroll_teleportation" => "Teleports you to another safe cell on this floor.",
            "scroll_identify" => "Choose an unidentified scroll or equipment item to reveal it.",
            "scroll_magic_mapping" => "Reveals the floor layout and hidden traps, but not enemy positions.",
            _ => string.Empty
        };
    }

    public string DisplayName(PrototypeInventoryItem item)
    {
        if (item == null) return string.Empty;
        var index = Array.IndexOf(Ids, item.ItemId);
        if (index >= 0)
        {
            var scrollName = known.Contains(item.ItemId) ? Names[index] : $"Scroll '{labels[item.ItemId]}'";
            return item.Quantity > 1 ? $"{scrollName} x{item.Quantity}" : scrollName;
        }
        var label = item.ItemId.Replace('_', ' ');
        if (item.UpgradeLevel > 0) label += $" +{item.UpgradeLevel}";
        if (item.EquipmentSlot != PrototypeEquipmentSlot.None)
            label += item.IsIdentified ? (item.IsCursed ? " (cursed)" : string.Empty) : " (?)";
        return label;
    }

    public bool CanTarget(string scrollId, PrototypeInventoryItem item)
    {
        if (item == null) return false;
        if (scrollId == "scroll_upgrade")
            return item.EquipmentSlot == PrototypeEquipmentSlot.Weapon && PrototypeMeleeWeapons.TryGet(item.ItemId, out _);
        if (scrollId == "scroll_identify")
            return IsScroll(item.ItemId) ? !IsKnown(item.ItemId) : item.EquipmentSlot != PrototypeEquipmentSlot.None && !item.IsIdentified;
        return false;
    }

    public bool Read(int scrollIndex, out bool selectTarget, out string message)
    {
        selectTarget = false;
        message = string.Empty;
        var scroll = inventory.GetSlot(scrollIndex);
        if (scroll == null || !IsScroll(scroll.ItemId)) { message = "No scroll selected."; return false; }
        if (RequiresTarget(scroll.ItemId))
        {
            for (var i = 0; i < inventory.Capacity; i++)
                if (i != scrollIndex && CanTarget(scroll.ItemId, inventory.GetSlot(i)))
                {
                    selectTarget = true;
                    message = scroll.ItemId == "scroll_upgrade" ? "Select a melee weapon to upgrade." : "Select an unidentified item.";
                    return true;
                }
            message = scroll.ItemId == "scroll_upgrade" ? "No melee weapon to upgrade." : "No unidentified item to identify.";
            return false;
        }

        var fog = FindAnyObjectByType<PrototypeFogOfWar>();
        switch (scroll.ItemId)
        {
            case "scroll_challenge":
                foreach (var enemy in FindObjectsByType<PrototypeEnemy>(FindObjectsInactive.Exclude)) enemy.AlertTo(player);
                message = "The challenge alerts every enemy on this floor!";
                break;
            case "scroll_remove_curse":
                for (var i = 0; i < inventory.Capacity; i++)
                {
                    var item = inventory.GetSlot(i);
                    if (item != null) item.IsCursed = false;
                }
                message = "Your equipment and backpack are cleansed.";
                break;
            case "scroll_teleportation":
                if (fog == null || !fog.TryTeleportPlayer(random)) { message = "No safe teleport destination."; return false; }
                message = "You teleport to another part of this floor.";
                break;
            case "scroll_magic_mapping":
                if (fog == null) { message = "There is no dungeon map to reveal."; return false; }
                fog.RevealEntireMap();
                message = "The map and hidden traps are revealed.";
                break;
            default: return false;
        }
        known.Add(scroll.ItemId);
        inventory.ConsumeOneAt(scrollIndex);
        return true;
    }

    public bool ReadOnTarget(int scrollIndex, int targetIndex, out string message)
    {
        var scroll = inventory.GetSlot(scrollIndex);
        var target = inventory.GetSlot(targetIndex);
        if (scroll == null || scrollIndex == targetIndex || !CanTarget(scroll.ItemId, target))
        {
            message = "Select an eligible item.";
            return false;
        }
        if (scroll.ItemId == "scroll_upgrade")
        {
            target.UpgradeLevel++;
            target.IsCursed = false;
            target.IsIdentified = true;
            message = $"Upgraded {DisplayName(target)}.";
        }
        else
        {
            if (IsScroll(target.ItemId)) known.Add(target.ItemId);
            else target.IsIdentified = true;
            message = $"Identified {DisplayName(target)}.";
        }
        known.Add(scroll.ItemId);
        inventory.ConsumeOneAt(scrollIndex);
        return true;
    }
}
