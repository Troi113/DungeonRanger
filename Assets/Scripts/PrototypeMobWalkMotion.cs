using UnityEngine;

/// <summary>
/// Presentation only: a code-driven walk for models without animation. While the parent
/// (the enemy root) moves, the model bobs up and down and rocks side to side, with the
/// step rate following movement speed. It settles back to rest when the parent stops.
/// PlayBite adds an attack on top: the mob rears back nose-up, then snaps forward and down.
/// </summary>
public sealed class PrototypeMobWalkMotion : MonoBehaviour
{
    [SerializeField] private float stepsPerUnit = 0.9f;
    [SerializeField] private float bobHeight = 0.18f;
    [SerializeField] private float rollDegrees = 7f;
    [SerializeField] private float pitchDegrees = 3f;
    [SerializeField] private float movingSpeedThreshold = 0.2f;
    [SerializeField] private float blendSpeed = 8f;
    [SerializeField] private float biteDuration = 0.35f;
    [SerializeField] private float biteReach = 1.2f;
    [SerializeField] private float biteRearBack = 0.4f;

    private Transform body;
    private Vector3 lastPosition;
    private float phase;
    private float blend;
    private float biteStart = -1f;

    /// <summary>Plays the bite attack (presentation only).</summary>
    public void PlayBite() => biteStart = Time.time;

    private void Awake()
    {
        body = transform.parent;
        lastPosition = body != null ? body.position : transform.position;
    }

    private void LateUpdate()
    {
        if (body == null) return;
        var deltaTime = Time.deltaTime;
        var moved = body.position - lastPosition;
        moved.y = 0f;
        lastPosition = body.position;
        // Paused games have no elapsed time; hold the current pose.
        if (deltaTime <= 0f) return;

        var speed = moved.magnitude / deltaTime;
        var target = speed > movingSpeedThreshold ? 1f : 0f;
        blend = Mathf.MoveTowards(blend, target, blendSpeed * deltaTime);
        phase += moved.magnitude * stepsPerUnit * Mathf.PI * 2f;
        if (blend <= 0f) phase = 0f;

        var step = Mathf.Sin(phase);
        BitePose(out var biteForward, out var biteLift, out var bitePitch);
        transform.localPosition = new Vector3(0f, Mathf.Abs(step) * bobHeight * blend + biteLift, biteForward);
        transform.localRotation = Quaternion.Euler(Mathf.Cos(phase * 2f) * pitchDegrees * blend + bitePitch, 0f, step * rollDegrees * blend);
    }

    /// <summary>Rear back nose-up (first 45%), snap forward nose-down (to 60%), then settle.</summary>
    private void BitePose(out float forward, out float lift, out float pitch)
    {
        forward = lift = pitch = 0f;
        if (biteStart < 0f) return;
        var t = (Time.time - biteStart) / biteDuration;
        if (t >= 1f) { biteStart = -1f; return; }
        if (t < 0.45f)
        {
            var w = Mathf.SmoothStep(0f, 1f, t / 0.45f);
            forward = -biteRearBack * w;
            lift = 0.25f * w;
            pitch = -22f * w;
        }
        else if (t < 0.6f)
        {
            var s = (t - 0.45f) / 0.15f;
            s = 1f - (1f - s) * (1f - s);
            forward = Mathf.Lerp(-biteRearBack, biteReach, s);
            lift = Mathf.Lerp(0.25f, 0.05f, s);
            pitch = Mathf.Lerp(-22f, 14f, s);
        }
        else
        {
            var r = 1f - Mathf.SmoothStep(0f, 1f, (t - 0.6f) / 0.4f);
            forward = biteReach * r;
            lift = 0.05f * r;
            pitch = 14f * r;
        }
    }
}
