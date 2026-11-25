using System.Collections;
using UnityEngine;

/// <summary>
/// A generated door that opens when the ranger enters its doorway.
/// Each door has a deterministic alert roll and only alerts enemies in the
/// room that owns that doorway.
/// </summary>
public sealed class PrototypeDoor : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float alertChance = 0.5f;
    [SerializeField] private float openingDuration = 0.25f;
    [SerializeField] private float openHeight = 2.6f;

    private Transform doorPanel;
    private Collider doorPanelCollider;
    private Rect ownerRoomBounds;
    private System.Random alertRandom;
    private bool initialized;
    private Vector3 closedPanelPosition;
    private Vector3 openPanelPosition;
    private Coroutine panelAnimation;

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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized || IsOpen)
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
        if (!IsOpen)
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
            yield return null;
        }

        doorPanel.localPosition = target;
        if (!IsOpen && doorPanelCollider != null)
        {
            doorPanelCollider.enabled = true;
        }

        panelAnimation = null;
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
