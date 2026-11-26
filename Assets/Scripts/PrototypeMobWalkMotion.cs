using UnityEngine;

/// <summary>
/// Presentation only: a code-driven walk for models without animation. While the parent
/// (the enemy root) moves, the model bobs up and down and rocks side to side, with the
/// step rate following movement speed. It settles back to rest when the parent stops.
/// </summary>
public sealed class PrototypeMobWalkMotion : MonoBehaviour
{
    [SerializeField] private float stepsPerUnit = 0.9f;
    [SerializeField] private float bobHeight = 0.18f;
    [SerializeField] private float rollDegrees = 7f;
    [SerializeField] private float pitchDegrees = 3f;
    [SerializeField] private float movingSpeedThreshold = 0.2f;
    [SerializeField] private float blendSpeed = 8f;

    private Transform body;
    private Vector3 lastPosition;
    private float phase;
    private float blend;

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
        transform.localPosition = new Vector3(0f, Mathf.Abs(step) * bobHeight * blend, 0f);
        transform.localRotation = Quaternion.Euler(Mathf.Cos(phase * 2f) * pitchDegrees * blend, 0f, step * rollDegrees * blend);
    }
}
