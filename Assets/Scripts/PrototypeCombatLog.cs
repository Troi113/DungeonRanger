using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A hidden log of combat events (attacks, hits, misses, damage taken, kills, damage from
/// effects). Gameplay code adds lines; PrototypeGameUI shows the newest ones once the player
/// has pressed E five times in a row. Keeps the last `Capacity` lines.
/// </summary>
public static class PrototypeCombatLog
{
    public const int Capacity = 60;
    private static readonly List<string> Lines = new();

    public static IReadOnlyList<string> Entries => Lines;

    public static void Add(string text)
    {
        var seconds = Mathf.FloorToInt(Time.time);
        Lines.Add($"[{seconds / 60:00}:{seconds % 60:00}] {text}");
        if (Lines.Count > Capacity) Lines.RemoveAt(0);
    }

    public static void Clear() => Lines.Clear();
}
