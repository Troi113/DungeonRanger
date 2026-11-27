using UnityEngine;

/// <summary>
/// Presentation only: a code-driven four-legged gait for the rat models, which have no rig or
/// animation. While the enemy root moves, the body bounds like a running rodent: each stride
/// it stretches out and rises (hind legs pushing), then lands and bunches up, nose dipping on
/// the landing. There is no side-to-side rocking. At rest it breathes and sniffs. The model
/// turns smoothly toward the direction of travel instead of snapping.
/// PlayBite adds an attack on top: the rat crouches back, then lunges forward stretched out,
/// head dipping into the bite, and settles.
/// </summary>
public sealed class PrototypeMobWalkMotion : MonoBehaviour
{
    [SerializeField] private float strideLength = 1.8f;
    [SerializeField] private float hopHeight = 0.12f;
    [SerializeField] private float pitchDegrees = 6f;
    [SerializeField] private float stretch = 0.08f;
    [SerializeField] private float movingSpeedThreshold = 0.2f;
    [SerializeField] private float blendSpeed = 8f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float biteDuration = 0.35f;
    [SerializeField] private float biteReach = 1.1f;
    [SerializeField] private float biteCrouch = 0.3f;

    private Transform body;
    private Vector3 lastPosition;
    private Quaternion facing;
    private float phase;
    private float blend;
    private float biteStart = -1f;

    /// <summary>Plays the bite attack (presentation only).</summary>
    public void PlayBite() => biteStart = Time.time;

    private void Awake()
    {
        body = transform.parent;
        lastPosition = body != null ? body.position : transform.position;
        facing = body != null ? body.rotation : transform.rotation;
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
        blend = Mathf.MoveTowards(blend, speed > movingSpeedThreshold ? 1f : 0f, blendSpeed * deltaTime);
        phase += moved.magnitude / strideLength * Mathf.PI * 2f;
        if (blend <= 0f) phase = 0f;

        // Bounding stride: s goes 0 (landed, bunched) -> 1 (stretched out in the air).
        var s = 0.5f - 0.5f * Mathf.Cos(phase);
        var lift = hopHeight * s * s * blend;
        var pitch = -Mathf.Sin(phase) * pitchDegrees * blend; // nose up while rising, down on landing
        var length = 1f + (s - 0.5f) * 2f * stretch * blend;
        var height = 1f - (s - 0.5f) * stretch * blend;

        // At rest: slow breathing and a little sniff.
        var idle = 1f - blend;
        var time = Time.time;
        height += Mathf.Sin(time * 3.1f) * 0.015f * idle;
        pitch += Mathf.Sin(time * 7.3f) * Mathf.Max(0f, Mathf.Sin(time * 0.9f)) * 2.5f * idle;

        BitePose(out var forward, out var biteLift, out var bitePitch, out var biteLength);

        // Turn toward the body's direction smoothly.
        facing = Quaternion.Slerp(facing, body.rotation, 1f - Mathf.Exp(-turnSpeed * deltaTime));
        transform.rotation = facing * Quaternion.Euler(pitch + bitePitch, 0f, 0f);
        transform.position = body.position + facing * new Vector3(0f, lift + biteLift, forward);
        transform.localScale = new Vector3(1f / Mathf.Sqrt(length * biteLength), height / biteLength, length * biteLength);
    }

    /// <summary>Crouch back (first 45%), lunge forward stretched with the head dipping (to 60%), then settle.</summary>
    private void BitePose(out float forward, out float lift, out float pitch, out float length)
    {
        forward = lift = pitch = 0f;
        length = 1f;
        if (biteStart < 0f) return;
        var t = (Time.time - biteStart) / biteDuration;
        if (t >= 1f) { biteStart = -1f; return; }
        if (t < 0.45f)
        {
            var w = Mathf.SmoothStep(0f, 1f, t / 0.45f);
            forward = -biteCrouch * w;
            lift = -0.06f * w;
            pitch = -6f * w;
            length = 1f - 0.08f * w;
        }
        else if (t < 0.6f)
        {
            var s = (t - 0.45f) / 0.15f;
            s = 1f - (1f - s) * (1f - s);
            forward = Mathf.Lerp(-biteCrouch, biteReach, s);
            lift = Mathf.Lerp(-0.06f, 0.04f, s);
            pitch = Mathf.Lerp(-6f, 12f, s);
            length = Mathf.Lerp(0.92f, 1.14f, s);
        }
        else
        {
            var r = 1f - Mathf.SmoothStep(0f, 1f, (t - 0.6f) / 0.4f);
            forward = biteReach * r;
            lift = 0.04f * r;
            pitch = 12f * r;
            length = 1f + 0.14f * r;
        }
    }
}
