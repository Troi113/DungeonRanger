using UnityEngine;

/// <summary>
/// Simple fixed-angle camera for the movement test.
/// </summary>
public sealed class PrototypeCameraFollow : MonoBehaviour
{
    public Transform Target { get; set; }
    public Vector3 Offset { get; set; } = new(0f, 9f, -8f);

    private void LateUpdate()
    {
        if (Target == null)
        {
            return;
        }

        transform.position = Target.position + Offset;
        transform.LookAt(Target.position + Vector3.up * 0.5f);
    }
}
