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
    [Tooltip("Seconds Space must be held, standing still, with nothing to interact with to search.")]
    [SerializeField] private float searchHoldSeconds = 1.5f;

    private PrototypePlayerMovement movement;
    private PrototypeInteractable currentTarget;
    private float nextTargetRefresh;
    private PrototypePlayerSearch search;
    private float searchHeld;
    private bool searchEligible;

    public PrototypeInteractable CurrentTarget => currentTarget;
    /// <summary>0 when not searching, rising to 1 while Space is held, standing still, with nothing nearby.</summary>
    public float SearchProgress => searchEligible ? Mathf.Clamp01(searchHeld / searchHoldSeconds) : 0f;
    public string SearchMessage => search != null ? search.Message : null;

    private void Awake()
    {
        movement = GetComponent<PrototypePlayerMovement>();
        search = GetComponent<PrototypePlayerSearch>();
    }

    private void Update()
    {
        if (PrototypeGameUI.IsGamePaused)
        {
            currentTarget = null;
            searchEligible = false;
            return;
        }

        var stats = GetComponent<PrototypePlayerStats>();
        if (stats != null && stats.IsDead)
        {
            currentTarget = null;
            searchEligible = false;
            return;
        }

        if (Time.time >= nextTargetRefresh)
        {
            nextTargetRefresh = Time.time + targetRefreshInterval;
            currentTarget = FindTarget();
        }

        if (Keyboard.current == null) return;
        var space = Keyboard.current.spaceKey;
        if (space.wasPressedThisFrame)
        {
            var hasTarget = currentTarget != null && currentTarget.isActiveAndEnabled && currentTarget.CanInteract(movement);
            if (hasTarget) currentTarget.Interact(movement);
            // A press with nothing to interact with starts a search if Space stays held.
            searchEligible = !hasTarget;
            searchHeld = 0f;
            currentTarget = FindTarget();
        }

        if (!space.isPressed)
        {
            searchEligible = false;
            searchHeld = 0f;
        }
        else if (searchEligible)
        {
            // Like Pixel Dungeon, searching takes time spent in place; walking restarts it.
            searchHeld = movement.IsMoving ? 0f : searchHeld + Time.deltaTime;
            if (searchHeld >= searchHoldSeconds)
            {
                searchEligible = false;
                if (search == null) search = GetComponent<PrototypePlayerSearch>();
                search?.Search();
            }
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
