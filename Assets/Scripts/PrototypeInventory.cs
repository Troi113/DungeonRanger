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
    public int UpgradeLevel;
    public bool IsCursed;
    public bool IsIdentified = true;
    public int Quantity = 1;
    public int Hits;            // enemy hits landed while unidentified (identifies weapons over time)

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

    [SerializeField, Range(0f, 1f)] private float equipCurseChance = 0.15f;

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

        if (PrototypeScrolls.IsScroll(item.ItemId) || PrototypePotions.IsPotion(item.ItemId))
        {
            for (var index = EquipmentSlotCount; index < SlotCount; index++)
            {
                if (slots[index] == null || slots[index].ItemId != item.ItemId) continue;
                slots[index].Quantity += item.Quantity;
                InventoryChanged?.Invoke();
                return true;
            }
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
        if (!IsValidIndex(inventoryIndex) || inventoryIndex < EquipmentSlotCount ||
            equipmentSlot == PrototypeEquipmentSlot.None ||
            (int)equipmentSlot > EquipmentSlotCount)
        {
            return false;
        }

        var item = slots[inventoryIndex];
        if (item == null || !CanEquip(item, equipmentSlot))
        {
            return false;
        }

        var targetIndex = (int)equipmentSlot - 1;
        if (slots[targetIndex] != null && slots[targetIndex].IsCursed) return false;
        // Swapping keeps the replaced equipment in the original backpack slot.
        slots[inventoryIndex] = slots[targetIndex];
        slots[targetIndex] = item;
        // Unidentified equipment may turn out cursed when worn; identifying it first makes it safe.
        if (!item.IsIdentified && !item.IsCursed && UnityEngine.Random.value < equipCurseChance) item.IsCursed = true;
        if (item.IsCursed) item.IsIdentified = true;
        InventoryChanged?.Invoke();
        return true;
    }

    public bool TryUnequip(int equipmentIndex)
    {
        if (equipmentIndex < 0 || equipmentIndex >= EquipmentSlotCount || slots[equipmentIndex] == null || slots[equipmentIndex].IsCursed) return false;
        for (var index = EquipmentSlotCount; index < SlotCount; index++)
        {
            if (slots[index] != null) continue;
            slots[index] = slots[equipmentIndex];
            slots[equipmentIndex] = null;
            InventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    private static bool CanEquip(PrototypeInventoryItem item, PrototypeEquipmentSlot target)
    {
        if (item.EquipmentSlot == PrototypeEquipmentSlot.ArtifactOrRingA ||
            item.EquipmentSlot == PrototypeEquipmentSlot.ArtifactOrRingB)
            return target == PrototypeEquipmentSlot.ArtifactOrRingA || target == PrototypeEquipmentSlot.ArtifactOrRingB;
        return item.EquipmentSlot != PrototypeEquipmentSlot.None && item.EquipmentSlot == target;
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

    /// <summary>Removes one unit from a slot (the whole item unless it is a stack) and returns it.</summary>
    public PrototypeInventoryItem TakeOneAt(int index)
    {
        var item = GetSlot(index);
        if (item == null) return null;
        if (item.Quantity <= 1) return RemoveAt(index);
        item.Quantity--;
        InventoryChanged?.Invoke();
        return new PrototypeInventoryItem(item.ItemId, item.EquipmentSlot)
        {
            UpgradeLevel = item.UpgradeLevel,
            IsCursed = item.IsCursed,
            IsIdentified = item.IsIdentified
        };
    }

    /// <summary>Replaces a slot's contents; used when restoring a saved game.</summary>
    public void SetSlot(int index, PrototypeInventoryItem item)
    {
        if (!IsValidIndex(index)) return;
        slots[index] = item;
        InventoryChanged?.Invoke();
    }

    public bool ConsumeOneAt(int index)
    {
        var item = GetSlot(index);
        if (item == null) return false;
        if (item.Quantity > 1)
        {
            item.Quantity--;
            InventoryChanged?.Invoke();
        }
        else RemoveAt(index);
        return true;
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

            return ConsumeOneAt(index);
        }

        return false;
    }

    private static bool IsValidIndex(int index)
    {
        return index >= 0 && index < SlotCount;
    }
}
