using System;
using UnityEngine;

public enum PrototypeEquipmentSlot
{
    None,
    Weapon,
    Armor,
    ArtifactOrRingA,
    ArtifactOrRingB
}

[Serializable]
public sealed class PrototypeInventoryItem
{
    public string ItemId;
    public PrototypeEquipmentSlot EquipmentSlot;

    public PrototypeInventoryItem(string itemId, PrototypeEquipmentSlot equipmentSlot = PrototypeEquipmentSlot.None)
    {
        ItemId = itemId;
        EquipmentSlot = equipmentSlot;
    }
}

/// <summary>
/// Twenty total slots, including the four equipment slots.
/// Slots 0-3 are weapon, armor, artifact/ring A, and artifact/ring B.
/// </summary>
public sealed class PrototypeInventory : MonoBehaviour
{
    public const int SlotCount = 20;
    private const int EquipmentSlotCount = 4;

    private PrototypeInventoryItem[] slots;

    public int Capacity => SlotCount;
    public event Action InventoryChanged;

    private void Awake()
    {
        slots = new PrototypeInventoryItem[SlotCount];
    }

    public PrototypeInventoryItem GetSlot(int index)
    {
        return IsValidIndex(index) ? slots[index] : null;
    }

    public bool TryAdd(PrototypeInventoryItem item)
    {
        if (item == null)
        {
            return false;
        }

        for (var index = EquipmentSlotCount; index < SlotCount; index++)
        {
            if (slots[index] == null)
            {
                slots[index] = item;
                InventoryChanged?.Invoke();
                return true;
            }
        }

        return false;
    }

    public bool TryEquip(int inventoryIndex, PrototypeEquipmentSlot equipmentSlot)
    {
        if (!IsValidIndex(inventoryIndex) ||
            equipmentSlot == PrototypeEquipmentSlot.None ||
            (int)equipmentSlot > EquipmentSlotCount)
        {
            return false;
        }

        var item = slots[inventoryIndex];
        if (item == null ||
            (item.EquipmentSlot != PrototypeEquipmentSlot.None && item.EquipmentSlot != equipmentSlot))
        {
            return false;
        }

        var targetIndex = (int)equipmentSlot - 1;
        if (slots[targetIndex] != null)
        {
            return false;
        }

        slots[targetIndex] = item;
        slots[inventoryIndex] = null;
        InventoryChanged?.Invoke();
        return true;
    }

    public PrototypeInventoryItem RemoveAt(int index)
    {
        if (!IsValidIndex(index))
        {
            return null;
        }

        var item = slots[index];
        slots[index] = null;
        if (item != null)
        {
            InventoryChanged?.Invoke();
        }

        return item;
    }

    public bool HasItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        foreach (var item in slots)
        {
            if (item != null && string.Equals(item.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryConsume(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        for (var index = 0; index < slots.Length; index++)
        {
            var item = slots[index];
            if (item == null || !string.Equals(item.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            slots[index] = null;
            InventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    private static bool IsValidIndex(int index)
    {
        return index >= 0 && index < SlotCount;
    }
}
