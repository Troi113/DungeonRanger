using System.Collections;
using UnityEngine;

/// <summary>
/// A generated door that opens when the ranger enters its doorway.
/// Each door has a deterministic alert roll and only alerts enemies in the
/// room that owns that doorway.
/// Doors are wooden (PrototypeDoorModel) and burn: fire on either side of the doorway, or a
/// burning ranger touching it, sets it alight; after `burnSeconds` it burns through and the
/// doorway stays open for good. A ranger in the doorway of a burning door keeps burning at the
/// full Burning count until they step out of it.
/// </summary>
public sealed class PrototypeDoor : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float alertChance = 0.5f;
    [SerializeField] private float openingDuration = 0.25f;
    [SerializeField] private float openHeight = 2.6f;
    [SerializeField] private float burnSeconds = 6f;

    private Transform doorPanel;
    private Collider doorPanelCollider;
    private Rect ownerRoomBounds;
    private System.Random alertRandom;
    private bool initialized;
    private Vector3 closedPanelPosition;
    private Vector3 openPanelPosition;
    private Coroutine panelAnimation;
    private GameObject heldOpenBy;
    private Transform model;
    private Vector3 modelClosedPosition;
    private PrototypeDoorFire fire;
    private float burnStart;

    public bool IsBurning { get; private set; }
    public bool IsBurnt { get; private set; }

    public bool IsOpen { get; private set; }
    public Vector2Int Cell { get; private set; }
    public Vector2Int Direction { get; private set; }

    public void Initialize(
        Transform panel,
        Collider panelCollider,
        Rect roomBounds,
        int alertSeed,
        Vector2Int cell,
        Vector2Int direction)
    {
        doorPanel = panel;
        doorPanelCollider = panelCollider;
        ownerRoomBounds = roomBounds;
        Cell = cell;
        Direction = direction;
        closedPanelPosition = panel != null ? panel.localPosition : Vector3.zero;
        openPanelPosition = closedPanelPosition + Vector3.up * openHeight;
        alertRandom = new System.Random(alertSeed);
        initialized = true;

        // The wooden door model replaces the look of the panel cube (its collider stays).
        if (panel != null)
        {
            model = PrototypeDoorModel.Build(transform, panel, direction.x != 0, alertSeed);
            modelClosedPosition = model.localPosition;
            var panelRenderer = panel.GetComponent<Renderer>();
            if (panelRenderer != null) panelRenderer.enabled = false;
        }
    }

    /// <summary>Sets the door on fire (fire blobs, a burning ranger). Nothing happens if it is already burning or gone.</summary>
    public void Ignite()
    {
        if (!initialized || IsBurning || IsBurnt) return;
        IsBurning = true;
        burnStart = Time.time;
        PrototypeCombatLog.Add("A door catches fire!");
        if (model != null && doorPanel != null)
        {
            var size = doorPanel.localScale;
            fire = PrototypeDoorFire.Start(model, Direction.x != 0 ? new Vector3(size.z, size.y, size.x) : size);
        }
    }

    /// <summary>A door that burned down earlier in the run (from a save): the doorway is open and empty.</summary>
    public void RestoreBurnt()
    {
        IsBurning = false;
        IsBurnt = true;
        IsOpen = true;
        if (doorPanelCollider != null) doorPanelCollider.enabled = false;
        if (model != null) Destroy(model.gameObject);
    }

    private void Update()
    {
        if (!IsBurning) return;
        var progress = (Time.time - burnStart) / burnSeconds;
        if (fire != null) fire.SetProgress(Mathf.Clamp01(progress));
        if (progress < 1f) return;
        // Burnt through: the doorway is open for good.
        IsBurning = false;
        IsBurnt = true;
        IsOpen = true;
        if (panelAnimation != null) StopCoroutine(panelAnimation);
        if (doorPanelCollider != null) doorPanelCollider.enabled = false;
        if (fire != null) fire.Collapse();
        PrototypeCombatLog.Add("The door burns down.");
    }

    private void OnTriggerStay(Collider other)
    {
        if (!initialized || IsBurnt) return;
        var statuses = other.GetComponentInParent<PrototypeStatusEffects>();
        if (statuses == null || other.GetComponentInParent<PrototypePlayerMovement>() == null) return;
        // Standing in a burning doorway holds Burning at its full count; a burning ranger lights the door.
        if (IsBurning) statuses.Ignite();
        else if (statuses.Has(PrototypeStatusEffectType.Burning)) Ignite();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized || IsOpen || IsBurnt)
        {
            return;
        }

        if (other.GetComponentInParent<PrototypePlayerMovement>() == null)
        {
            return;
        }

        Open();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!initialized || other.GetComponentInParent<PrototypePlayerMovement>() == null)
        {
            return;
        }

        Close();
    }

    /// <summary>Opens the door and keeps it open while an item lies in the doorway.</summary>
    public void OpenAndHold(GameObject blocker)
    {
        heldOpenBy = blocker;
        if (!IsOpen) Open();
    }

    private void Open()
    {
        IsOpen = true;
        if (doorPanelCollider != null)
        {
            doorPanelCollider.enabled = false;
        }

        if (alertRandom != null && alertRandom.NextDouble() < alertChance)
        {
            AlertEnemiesInOwnerRoom();
        }

        StartPanelAnimation(openPanelPosition);
    }

    private void Close()
    {
        if (!IsOpen || heldOpenBy != null || IsBurnt)
        {
            return;
        }

        IsOpen = false;
        StartPanelAnimation(closedPanelPosition);
    }

    private void StartPanelAnimation(Vector3 target)
    {
        if (panelAnimation != null)
        {
            StopCoroutine(panelAnimation);
        }

        panelAnimation = StartCoroutine(AnimatePanel(target));
    }

    private IEnumerator AnimatePanel(Vector3 target)
    {
        if (doorPanel == null)
        {
            yield break;
        }

        var start = doorPanel.localPosition;
        var elapsed = 0f;

        while (elapsed < openingDuration)
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, openingDuration));
            doorPanel.localPosition = Vector3.Lerp(start, target, progress);
            SyncModel();
            yield return null;
        }

        doorPanel.localPosition = target;
        SyncModel();
        if (!IsOpen && !IsBurnt && doorPanelCollider != null)
        {
            doorPanelCollider.enabled = true;
        }

        panelAnimation = null;
    }

    /// <summary>The model slides with the (invisible) panel when the door opens and closes.</summary>
    private void SyncModel()
    {
        if (model != null && !IsBurnt) model.localPosition = modelClosedPosition + (doorPanel.localPosition - closedPanelPosition);
    }

    private void AlertEnemiesInOwnerRoom()
    {
        foreach (var enemy in FindObjectsByType<PrototypeEnemy>(FindObjectsInactive.Exclude))
        {
            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            var enemyPosition = enemy.transform.position;
            if (ownerRoomBounds.Contains(new Vector2(enemyPosition.x, enemyPosition.z)))
            {
                enemy.WakeUp();
            }
        }
    }
}
