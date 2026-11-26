using UnityEngine;

/// <summary>
/// An item thrown from the inventory. It flies straight along the ranger's facing direction
/// and lands as a pickup:
/// - on a wall, in the cell just before the wall;
/// - on a closed door, under the door, which it opens and holds open;
/// - on an untriggered trap, on top of the trap, which it sets off.
/// Enemies are not hit.
/// </summary>
public sealed class PrototypeThrownItem : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float maxDistance = 60f;
    [SerializeField] private float radius = 0.2f;
    [SerializeField] private float spinSpeed = 720f;

    private PrototypeInventoryItem item;
    private Transform landingParent;
    private Vector3 direction;
    private float travelled;
    private Transform visual;
    private bool landed;

    public static void Throw(PrototypeInventoryItem item, PrototypePlayerMovement thrower, Transform landingParent)
    {
        var direction = thrower.FacingDirection;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;

        var obj = new GameObject($"Thrown_{item.ItemId}");
        obj.transform.SetParent(landingParent, true);
        obj.transform.position = new Vector3(thrower.transform.position.x, 1f, thrower.transform.position.z);
        var thrown = obj.AddComponent<PrototypeThrownItem>();
        thrown.item = item;
        thrown.landingParent = landingParent;
        thrown.direction = direction;

        PrototypeItemCatalog.Describe(item.ItemId, out var label, out var color);
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Thrown_Visual";
        cube.transform.SetParent(obj.transform, false);
        cube.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
        cube.GetComponent<Collider>().enabled = false;
        cube.GetComponent<Renderer>().sharedMaterial = PrototypeBootstrap.CreateMaterial($"Thrown {label} Material", color);
        thrown.visual = cube.transform;

        // Thrown point-blank against a wall: it drops in the ranger's own cell.
        foreach (var overlap in Physics.OverlapSphere(obj.transform.position + direction * 0.6f, thrown.radius,
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (!IsWall(overlap)) continue;
            thrown.Land(CellCenter(obj.transform.position));
            return;
        }
    }

    private void Update()
    {
        if (landed || PrototypeGameUI.IsGamePaused) return;

        var step = Mathf.Min(speed * Time.deltaTime, maxDistance - travelled);
        var origin = transform.position;
        RaycastHit? nearest = null;
        foreach (var hit in Physics.SphereCastAll(origin, radius, direction, step, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (!IsStopper(hit.collider)) continue;
            if (nearest == null || hit.distance < nearest.Value.distance) nearest = hit;
        }

        // Traps are found by the grid cells the item passes over, so a fast throw cannot skip one.
        var trap = FindTrapAlong(origin, step, out var trapDistance);
        if (trap != null && (!nearest.HasValue || trapDistance <= nearest.Value.distance))
        {
            trap.ActivateByItem();
            Land(new Vector3(trap.transform.position.x, 0f, trap.transform.position.z));
            return;
        }

        if (nearest.HasValue)
        {
            HandleImpact(nearest.Value);
            return;
        }

        transform.position = origin + direction * step;
        travelled += step;
        if (visual != null) visual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        if (travelled >= maxDistance - 0.001f) Land(CellCenter(transform.position));
    }

    /// <summary>Finishes the flight immediately (used when saving mid-throw).</summary>
    public void LandNow()
    {
        if (!landed) Land(CellCenter(transform.position));
    }

    /// <summary>First untriggered trap whose cell this step passes over, sampled every quarter unit.</summary>
    private PrototypeTrap FindTrapAlong(Vector3 origin, float step, out float distance)
    {
        const float sampleSpacing = 0.25f;
        for (distance = 0f; distance <= step + 0.0001f; distance += sampleSpacing)
        {
            var cell = PrototypeLevelGenerator.WorldToGrid(origin + direction * Mathf.Min(distance, step));
            if (PrototypeTrap.TryGetAt(cell, out var trap) && trap != null && !trap.IsActivated) return trap;
        }
        distance = float.MaxValue;
        return null;
    }

    private static bool IsStopper(Collider collider)
    {
        if (IsWall(collider)) return true;
        return collider.name == "Door_Panel" && collider.enabled;
    }

    private static bool IsWall(Collider collider) =>
        !collider.isTrigger && (collider.name.StartsWith("Wall_") || collider.name.StartsWith("Frame_"));

    private void HandleImpact(RaycastHit hit)
    {
        if (hit.collider.name == "Door_Panel")
        {
            var door = hit.collider.GetComponentInParent<PrototypeDoor>();
            var doorPosition = door != null ? door.transform.position : hit.point;
            var pickup = Land(new Vector3(doorPosition.x, 0f, doorPosition.z));
            if (door != null && pickup != null) door.OpenAndHold(pickup.gameObject);
            return;
        }

        // Wall: rest in the cell just before the point of impact.
        var beforeWall = transform.position + direction * Mathf.Max(0f, hit.distance - 0.5f);
        Land(CellCenter(beforeWall));
    }

    private static Vector3 CellCenter(Vector3 world) =>
        PrototypeLevelGenerator.GridToWorld(PrototypeLevelGenerator.WorldToGrid(world), 0f);

    private PrototypeItemPickup Land(Vector3 position)
    {
        if (landed) return null;
        landed = true;
        var pickup = PrototypeItemPickup.CreateForItem(landingParent, position, item);
        gameObject.SetActive(false);
        Destroy(gameObject);
        return pickup;
    }
}
