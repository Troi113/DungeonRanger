using System;
using UnityEngine;

/// <summary>
/// Finding hidden traps, adapted from Pixel Dungeon's Hero.search():
/// - Deliberate search (hold Space, see PrototypePlayerInteraction) always reveals every hidden
///   trap in a square of `searchDistanceCells` around the ranger.
/// - Passive noticing: each time the ranger enters a new cell, each hidden trap within
///   `passiveDistanceCells` has an `awareness` chance to be noticed (Pixel Dungeon's starting
///   awareness is 0.1).
/// Both only reach cells the ranger can currently see.
/// </summary>
[RequireComponent(typeof(PrototypePlayerMovement))]
public sealed class PrototypePlayerSearch : MonoBehaviour
{
    [SerializeField] private int searchDistanceCells = 6;
    [SerializeField] private int passiveDistanceCells = 1;
    [SerializeField, Range(0f, 1f)] private float awareness = 0.1f;
    [SerializeField] private float messageSeconds = 2.5f;

    private PrototypePlayerStats stats;
    private PrototypeFogOfWar fog;
    private Func<Vector2Int, bool> isVisible;
    private Vector2Int lastCell;
    private bool hasCell;
    private string message;
    private float messageUntil;

    public string Message => Time.time < messageUntil ? message : null;

    private void Awake()
    {
        stats = GetComponent<PrototypePlayerStats>();
        isVisible = IsVisible;
    }

    private void Update()
    {
        if (PrototypeGameUI.IsGamePaused || (stats != null && stats.IsDead)) return;

        var cell = PrototypeLevelGenerator.WorldToGrid(transform.position);
        if (!hasCell || cell == lastCell)
        {
            lastCell = cell;
            hasCell = true;
            return;
        }

        lastCell = cell;
        if (PrototypeTrap.SearchAround(cell, passiveDistanceCells, awareness, isVisible) > 0)
            Show("You noticed something.");
    }

    /// <summary>A deliberate search: always finds hidden traps in range.</summary>
    public void Search()
    {
        var cell = PrototypeLevelGenerator.WorldToGrid(transform.position);
        var found = PrototypeTrap.SearchAround(cell, searchDistanceCells, 1f, isVisible);
        Show(found > 0 ? "You noticed something." : "You search but find nothing.");
    }

    private void Show(string text)
    {
        message = text;
        PrototypeGameLog.Add(text);
        messageUntil = Time.time + messageSeconds;
    }

    private bool IsVisible(Vector2Int cell)
    {
        if (fog == null || !fog.isActiveAndEnabled) fog = FindAnyObjectByType<PrototypeFogOfWar>();
        return fog == null || fog.IsCellVisible(cell);
    }
}
