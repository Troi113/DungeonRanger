using UnityEngine;

/// <summary>
/// A locked chest opened from the front with a key in the player's inventory.
/// </summary>
public sealed class PrototypeChest : PrototypeInteractable
{
    private const string KeyItemId = "key";

    private string rewardId;
    private string rewardName;
    private PrototypeEquipmentSlot rewardSlot;
    private Color rewardColor;
    private Renderer chestRenderer;
    private bool opened;

    public override string InteractionPrompt => opened
        ? "Chest is open"
        : "Space: Open chest with key";

    public override bool CanInteract(PrototypePlayerMovement player)
    {
        return !opened && player != null && player.GetComponent<PrototypeInventory>() != null;
    }

    public void Initialize(
        Renderer renderer,
        string containedItemId,
        string containedItemName,
        PrototypeEquipmentSlot containedItemSlot,
        Color containedItemColor)
    {
        chestRenderer = renderer;
        rewardId = containedItemId;
        rewardName = containedItemName;
        rewardSlot = containedItemSlot;
        rewardColor = containedItemColor;
    }

    public override void Interact(PrototypePlayerMovement player)
    {
        if (opened || player == null)
        {
            return;
        }

        var inventory = player.GetComponent<PrototypeInventory>();
        if (inventory == null || !inventory.TryConsume(KeyItemId))
        {
            return;
        }

        opened = true;
        if (chestRenderer != null)
        {
            chestRenderer.material.color = new Color(0.22f, 0.5f, 0.2f);
        }

        PrototypeItemPickup.CreateRuntime(
            transform.parent,
            transform.position + transform.forward * 1.5f,
            $"Item_{rewardId}",
            rewardId,
            rewardName,
            rewardSlot,
            rewardColor);
    }
}
