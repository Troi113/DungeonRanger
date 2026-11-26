using UnityEngine;

/// <summary>
/// Simple fixed-angle camera for the movement test.
/// </summary>
public sealed class PrototypeCameraFollow : MonoBehaviour
{
    public Transform Target { get; set; }
    public Vector3 Offset { get; set; } = new(0f, 9f, -8f);

    private static float shakeStrength;
    private static float shakeUntil;
    private static float shakeLength = 0.1f;

    /// <summary>Shakes the camera; stronger shakes override weaker ones. Uses unscaled time.</summary>
    public static void AddShake(float strength, float seconds)
    {
        var remaining = Mathf.Max(0f, shakeUntil - Time.unscaledTime);
        if (strength < shakeStrength && remaining > 0f) return;
        shakeStrength = strength;
        shakeLength = Mathf.Max(0.01f, seconds);
        shakeUntil = Time.unscaledTime + seconds;
    }

    private void LateUpdate()
    {
        if (Target == null)
        {
            return;
        }

        transform.position = Target.position + Offset;
        transform.LookAt(Target.position + Vector3.up * 0.5f);

        var remaining = shakeUntil - Time.unscaledTime;
        if (remaining <= 0f) return;
        var strength = shakeStrength * (remaining / shakeLength);
        var time = Time.unscaledTime * 60f;
        transform.position += transform.right * (Mathf.PerlinNoise(time, 0.3f) - 0.5f) * 2f * strength
                            + transform.up * (Mathf.PerlinNoise(0.7f, time) - 0.5f) * 2f * strength;
    }
}
