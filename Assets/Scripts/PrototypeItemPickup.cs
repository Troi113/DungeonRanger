using UnityEngine;

/// <summary>
/// A simple world item picked up with Space.
/// </summary>
public sealed class PrototypeItemPickup : PrototypeInteractable
{
    private PrototypeInventoryItem item;
    private string displayName;

    public string ItemId => item?.ItemId;
    public string DisplayName => displayName;
    public PrototypeInventoryItem Item => item;
    /// <summary>Order among a floor's generated pickups, or -1 for dropped/thrown/chest items.</summary>
    public int GeneratedIndex { get; set; } = -1;
    public override bool RequiresFacing => false;
    public override string InteractionPrompt => $"Space: Pick up {displayName}";

    public void Initialize(string id, string label, PrototypeEquipmentSlot slot)
    {
        item = new PrototypeInventoryItem(id, slot);
        displayName = label;
    }

    public override void Interact(PrototypePlayerMovement player)
    {
        var inventory = player != null ? player.GetComponent<PrototypeInventory>() : null;
        if (!gameObject.activeInHierarchy || inventory == null || item == null || !inventory.TryAdd(item))
        {
            return;
        }

        PrototypeGameLog.Add($"You pick up {displayName}.");
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    public void SetEquipmentTraits(bool isCursed, bool isIdentified)
    {
        item.IsCursed = isCursed;
        item.IsIdentified = isIdentified;
    }

    /// <summary>Places an existing inventory item (keeping upgrades, curse, and stack size) on the floor.</summary>
    public static PrototypeItemPickup CreateForItem(Transform parent, Vector3 position, PrototypeInventoryItem inventoryItem)
    {
        PrototypeItemCatalog.Describe(inventoryItem.ItemId, out var label, out var color);
        var pickup = CreateRuntime(parent, position, $"Item_{inventoryItem.ItemId}", inventoryItem.ItemId, label,
            inventoryItem.EquipmentSlot, color);
        pickup.item = inventoryItem;
        return pickup;
    }

    private static readonly Color DropGlow = new(1f, 0.85f, 0.3f);

    public static PrototypeItemPickup CreateRuntime(
        Transform parent,
        Vector3 position,
        string objectName,
        string id,
        string label,
        PrototypeEquipmentSlot slot,
        Color color)
    {
        var itemObject = new GameObject(objectName);
        itemObject.transform.SetParent(parent, false);
        itemObject.transform.position = position;

        // The item's own model when it has one (weapons, potions, food, key), else a colored block.
        if (PrototypeItemVisuals.CreateOnFloor(id, itemObject.transform) == null)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Item_Visual";
            visual.transform.SetParent(itemObject.transform, false);
            // Twice the original block size, like the item models.
            visual.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            visual.transform.localScale = new Vector3(1.4f, 2.2f, 1.4f);
            if (PrototypeScrolls.IsScroll(id))
            {
                visual.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                visual.transform.localScale = new Vector3(1.6f, 0.8f, 2.2f);
            }
            visual.GetComponent<Renderer>().sharedMaterial = CreateMaterial($"{label} Material", color);
            var visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                visualCollider.enabled = false;
            }
        }

        // A soft pulsing yellow glow marks every drop; weapons glow a little brighter.
        var isWeapon = slot == PrototypeEquipmentSlot.Weapon;
        PrototypeGlowPulse.Add(itemObject, null, DropGlow, 0f, isWeapon ? 2.4f : 1.6f, isWeapon ? 4.2f : 3.5f, new Vector3(0f, 1.2f, 0f));

        var pickup = itemObject.AddComponent<PrototypeItemPickup>();
        pickup.Initialize(id, label, slot);
        return pickup;
    }

    private static Material CreateMaterial(string materialName, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader)
        {
            name = materialName,
            color = color
        };
        material.SetFloat("_Smoothness", 0f);
        return material;
    }
}
