using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A hidden log of everything that happens to the ranger except movement: combat, damage and
/// its sources, traps, searching, items picked up, used, identified or destroyed, scroll and
/// potion results, level-ups and floors. Every on-screen notice is logged too. PrototypeGameUI
/// shows the newest lines once the player has pressed E five times in a row. The same text
/// added twice within a second is only kept once. Keeps the last `Capacity` lines.
/// </summary>
public static class PrototypeGameLog
{
    public const int Capacity = 100;
    private static string lastText;
    private static float lastTime = -10f;
    private static readonly List<string> Lines = new();

    public static IReadOnlyList<string> Entries => Lines;

    public static void Add(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (text == lastText && Time.time - lastTime < 1f) return;
        lastText = text;
        lastTime = Time.time;
        var seconds = Mathf.FloorToInt(Time.time);
        Lines.Add($"[{seconds / 60:00}:{seconds % 60:00}] {text}");
        if (Lines.Count > Capacity) Lines.RemoveAt(0);
    }

    public static void Clear() => Lines.Clear();
}
