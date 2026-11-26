using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class TemporaryArmorCheck
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static TemporaryArmorCheck() { EditorApplication.update += Check; }

    private static void Check()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "DungeonGameplay") return;
        var hero = UnityEngine.Object.FindAnyObjectByType<PrototypePlayerMovement>();
        var run = UnityEngine.Object.FindAnyObjectByType<PrototypeDungeonRun>();
        if (hero == null || run == null) return;
        EditorApplication.update -= Check;
        try
        {
            var inventory = hero.GetComponent<PrototypeInventory>();
            var stats = hero.GetComponent<PrototypePlayerStats>();
            var scrolls = hero.GetComponent<PrototypeScrolls>();
            var pickups = UnityEngine.Object.FindObjectsByType<PrototypeItemPickup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var armorCount = 0;
            foreach (var pickup in pickups) if (PrototypeArmor.TryGet(pickup.ItemId, out _)) armorCount++;
            Require(armorCount == 2 && PrototypeArmor.Count == 5, "armor generation and tiers");
            stats.TakePhysicalDamage(1);
            Require(stats.CurrentHealth == 19, "unarmored damage");
            stats.Heal(20);

            typeof(PrototypeInventory).GetField("equipmentCurseChance", Private).SetValue(inventory, 1f);
            var cloth = new PrototypeInventoryItem("cloth_armor", PrototypeEquipmentSlot.Armor) { IsIdentified = false };
            var clothIndex = Add(inventory, cloth);
            Require(inventory.TryEquip(clothIndex, PrototypeEquipmentSlot.Armor) && cloth.IsCursed && cloth.IsIdentified && !inventory.TryUnequip(1), "armor curse on equip");
            var replacement = new PrototypeInventoryItem("leather_armor", PrototypeEquipmentSlot.Armor) { IsIdentified = false };
            var replacementIndex = Add(inventory, replacement);
            Require(!inventory.TryEquip(replacementIndex, PrototypeEquipmentSlot.Armor) && !replacement.CurseRollResolved, "cursed armor swap");
            var cleanseIndex = Add(inventory, new PrototypeInventoryItem("scroll_remove_curse"));
            Require(scrolls.Read(cleanseIndex, out _, out _) && !cloth.IsCursed && !replacement.IsCursed && replacement.CurseRollResolved, "cleanse armor");
            Require(inventory.TryUnequip(1), "uncursed armor cannot be removed");
            clothIndex = IndexOf(inventory, cloth);
            Require(inventory.TryEquip(clothIndex, PrototypeEquipmentSlot.Armor) && !cloth.IsCursed, "cleaned armor cursed again");

            var weapon = new PrototypeInventoryItem("sword", PrototypeEquipmentSlot.Weapon) { IsIdentified = false };
            var weaponIndex = Add(inventory, weapon);
            Require(inventory.TryEquip(weaponIndex, PrototypeEquipmentSlot.Weapon) && weapon.IsCursed && !inventory.TryUnequip(0), "weapon curse on equip");
            var upgradeIndex = Add(inventory, new PrototypeInventoryItem("scroll_upgrade"));
            Require(scrolls.Read(upgradeIndex, out var select, out _) && select && scrolls.ReadOnTarget(upgradeIndex, 1, out _), "upgrade armor");
            Require(cloth.UpgradeLevel == 1 && Mathf.Abs(PrototypeArmor.Protection(cloth) - 0.15f) < 0.001f, "upgraded armor protection");
            upgradeIndex = Add(inventory, new PrototypeInventoryItem("scroll_upgrade"));
            Require(scrolls.ReadOnTarget(upgradeIndex, 0, out _) && !weapon.IsCursed && weapon.CurseRollResolved && inventory.TryUnequip(0), "upgrade uncurse weapon");

            var oldHealth = stats.CurrentHealth;
            stats.TakeDamage(3);
            Require(stats.CurrentHealth == oldHealth - 3, "armor incorrectly blocked nonphysical damage");
            stats.Heal(20);
            typeof(PrototypeInventory).GetField("equipmentCurseChance", Private).SetValue(inventory, 0f);
            Require(inventory.TryUnequip(1), "armor replacement setup");
            var plate = new PrototypeInventoryItem("plate_armor", PrototypeEquipmentSlot.Armor);
            var plateIndex = Add(inventory, plate);
            Require(inventory.TryEquip(plateIndex, PrototypeEquipmentSlot.Armor), "equip plate");
            stats.InitializeProtectionRandom(77);
            var plateBlocked = CountBlocks(stats, 400);
            Require(inventory.TryUnequip(1), "plate unequip");
            clothIndex = IndexOf(inventory, cloth);
            Require(inventory.TryEquip(clothIndex, PrototypeEquipmentSlot.Armor), "equip cloth again");
            stats.InitializeProtectionRandom(77);
            var clothBlocked = CountBlocks(stats, 400);
            Require(plateBlocked > clothBlocked && clothBlocked > 0 && plateBlocked < 400, "armor mitigation and balance");

            var identifyItem = new PrototypeInventoryItem("mail_armor", PrototypeEquipmentSlot.Armor) { IsIdentified = false };
            var identifyItemIndex = Add(inventory, identifyItem);
            var identify = Add(inventory, new PrototypeInventoryItem("scroll_identify"));
            Require(scrolls.ReadOnTarget(identify, identifyItemIndex, out _) && identifyItem.IsIdentified && identifyItem.CurseRollResolved, "identify armor");
            var countCursed = 0;
            typeof(PrototypeInventory).GetField("equipmentCurseChance", Private).SetValue(inventory, 0.2f);
            inventory.InitializeCurseRandom(12345);
            for (var i = 0; i < 1000; i++)
            {
                var item = new PrototypeInventoryItem("knife", PrototypeEquipmentSlot.Weapon);
                inventory.ResolveCurse(item);
                if (item.IsCursed) countCursed++;
            }
            Require(countCursed > 150 && countCursed < 250, "curse chance");
            run.Descend();
            run.Ascend();
            Require(inventory.GetSlot(1) == cloth && cloth.UpgradeLevel == 1 && !cloth.IsCursed, "armor across floors");

            for (var i = 0; i < inventory.Capacity; i++) inventory.RemoveAt(i);
            inventory.TryAdd(new PrototypeInventoryItem("ranger_bow"));
            inventory.TryAdd(new PrototypeInventoryItem("cloth_armor", PrototypeEquipmentSlot.Armor) { IsIdentified = false });
            inventory.TryAdd(new PrototypeInventoryItem("scroll_remove_curse"));
            typeof(PrototypeInventory).GetField("equipmentCurseChance", Private).SetValue(inventory, 1f);
            Debug.Log("ARMOR_TEST_PASS: five armor types, mitigation, curse equip and reveal, scrolls, upgrade, floor persistence");
        }
        catch (Exception error) { Debug.LogError("ARMOR_TEST_FAIL: " + error); }
    }

    private static int CountBlocks(PrototypePlayerStats stats, int count)
    {
        var blocked = 0;
        for (var i = 0; i < count; i++)
        {
            var before = stats.CurrentHealth;
            stats.TakePhysicalDamage(1);
            if (stats.CurrentHealth == before) blocked++;
            stats.Heal(20);
        }
        return blocked;
    }

    private static int Add(PrototypeInventory inventory, PrototypeInventoryItem item)
    {
        Require(inventory.TryAdd(item), "inventory full");
        return IndexOf(inventory, item);
    }

    private static int IndexOf(PrototypeInventory inventory, PrototypeInventoryItem item)
    {
        for (var i = 0; i < inventory.Capacity; i++) if (inventory.GetSlot(i) == item) return i;
        throw new Exception("item not found");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
