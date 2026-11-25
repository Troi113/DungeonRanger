using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Finds nearby interactables and routes the Space key to the closest valid one.
/// Items can be picked up from nearby; chests require the player to face them.
/// </summary>
[RequireComponent(typeof(PrototypePlayerMovement), typeof(PrototypeInventory))]
public sealed class PrototypePlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 4.5f;
    [SerializeField, Range(0f, 1f)] private float facingDot = 0.2f;
    [SerializeField] private float targetRefreshInterval = 0.1f;

    private PrototypePlayerMovement movement;
    private PrototypeInteractable currentTarget;
    private float nextTargetRefresh;

    public PrototypeInteractable CurrentTarget => currentTarget;

    private void Awake()
    {
        movement = GetComponent<PrototypePlayerMovement>();
    }

    private void Update()
    {
        if (PrototypeGameUI.IsGamePaused)
        {
            currentTarget = null;
            return;
        }

        var stats = GetComponent<PrototypePlayerStats>();
        if (stats != null && stats.IsDead)
        {
            currentTarget = null;
            return;
        }

        if (Time.time >= nextTargetRefresh)
        {
            nextTargetRefresh = Time.time + targetRefreshInterval;
            currentTarget = FindTarget();
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            currentTarget?.Interact(movement);
            currentTarget = FindTarget();
        }
    }

    private PrototypeInteractable FindTarget()
    {
        var candidates = FindObjectsByType<PrototypeInteractable>(FindObjectsInactive.Exclude);
        PrototypeInteractable best = null;
        var bestDistance = float.MaxValue;
        var playerPosition = transform.position;

        foreach (var candidate in candidates)
        {
            if (candidate == null || !candidate.CanInteract(movement))
            {
                continue;
            }

            var offset = candidate.transform.position - playerPosition;
            offset.y = 0f;
            var distance = offset.magnitude;
            if (distance > interactionDistance)
            {
                continue;
            }

            if (candidate.RequiresFacing &&
                (distance < 0.01f || Vector3.Dot(movement.FacingDirection, offset.normalized) < facingDot))
            {
                continue;
            }

            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }
}
