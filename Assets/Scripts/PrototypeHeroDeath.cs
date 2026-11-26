using System.Collections;
using UnityEngine;

/// <summary>
/// Presentation only: the ranger's death. A hit-stop and red flash, then slow motion while the
/// model plays a death clip and the camera pushes in; the game-over screen fades in afterwards
/// (PrototypeGameUI reads DeathTime / ScreenDelay).
/// </summary>
[RequireComponent(typeof(PrototypePlayerStats))]
public sealed class PrototypeHeroDeath : MonoBehaviour
{
    /// <summary>Seconds (real time) from death until the game-over screen starts to fade in.</summary>
    public const float ScreenDelay = 1.8f;

    [SerializeField] private float slowMotionScale = 0.3f;
    [SerializeField] private float slowMotionSeconds = 1.2f;
    [SerializeField] private float cameraPushIn = 0.55f;

    /// <summary>Real time of the most recent death, or -1 while alive.</summary>
    public static float DeathTime { get; private set; } = -1f;

    private PrototypePlayerStats stats;

    private void Awake()
    {
        stats = GetComponent<PrototypePlayerStats>();
        stats.Died += OnDied;
        DeathTime = -1f;
    }

    private void OnDestroy()
    {
        if (stats != null) stats.Died -= OnDied;
    }

    private void OnDied()
    {
        DeathTime = Time.unscaledTime;
        PrototypeGameFeel.Shake(0.5f, 0.4f);
        PrototypeGameFeel.Sparks(transform.position + Vector3.up, new Color(0.85f, 0.1f, 0.1f), 30);
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        // Short freeze, then slow motion while the ranger falls.
        yield return new WaitForSecondsRealtime(0.15f); // after any hit-stop from the killing blow
        if (!PrototypeGameUI.IsGamePaused) Time.timeScale = slowMotionScale;
        var follow = Camera.main != null ? Camera.main.GetComponent<PrototypeCameraFollow>() : null;
        var startOffset = follow != null ? follow.Offset : Vector3.zero;
        if (follow != null && !savedOffset.HasValue) savedOffset = startOffset;
        var elapsed = 0f;
        while (elapsed < slowMotionSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            if (follow != null) follow.Offset = Vector3.Lerp(startOffset, startOffset * cameraPushIn, Mathf.SmoothStep(0f, 1f, elapsed / slowMotionSeconds));
            yield return null;
        }
        if (!PrototypeGameUI.IsGamePaused) Time.timeScale = 1f;
    }

    private static Vector3? savedOffset;

    /// <summary>Undoes the death camera push-in (called when a new run starts).</summary>
    public static void RestoreCamera()
    {
        var follow = Camera.main != null ? Camera.main.GetComponent<PrototypeCameraFollow>() : null;
        if (follow != null && savedOffset.HasValue) follow.Offset = savedOffset.Value;
        savedOffset = null;
        DeathTime = -1f;
    }
}
