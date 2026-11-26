using UnityEngine;

/// <summary>
/// A simple world item picked up with Space.
/// </summary>
public sealed class PrototypeItemPickup : PrototypeInteractable
{
    private string itemId;
    private string displayName;
    private PrototypeEquipmentSlot equipmentSlot;
    private bool cursed;
    private bool identified = true;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public override bool RequiresFacing => false;
    public override string InteractionPrompt => $"Space: Pick up {displayName}";

    public void Initialize(string id, string label, PrototypeEquipmentSlot slot)
    {
        itemId = id;
        displayName = label;
        equipmentSlot = slot;
    }

    public override void Interact(PrototypePlayerMovement player)
    {
        var inventory = player != null ? player.GetComponent<PrototypeInventory>() : null;
        var item = new PrototypeInventoryItem(itemId, equipmentSlot) { IsCursed = cursed, IsIdentified = identified };
        if (!gameObject.activeInHierarchy || inventory == null || !inventory.TryAdd(item))
        {
            return;
        }

        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    public void SetEquipmentTraits(bool isCursed, bool isIdentified)
    {
        cursed = isCursed;
        identified = isIdentified;
    }

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

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Item_Visual";
        visual.transform.SetParent(itemObject.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        visual.transform.localScale = new Vector3(0.7f, 1.1f, 0.7f);
        if (PrototypeScrolls.IsScroll(id))
        {
            visual.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            visual.transform.localScale = new Vector3(0.8f, 0.4f, 1.1f);
        }
        visual.GetComponent<Renderer>().sharedMaterial = CreateMaterial($"{label} Material", color);
        var visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
        {
            visualCollider.enabled = false;
        }

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
