using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Lightweight prototype HUD. It intentionally uses immediate-mode GUI so the
/// gameplay loop can be tested before committing to a final UI art direction.
/// </summary>
public sealed class PrototypeGameUI : MonoBehaviour
{
    private static readonly string[] EquipmentLabels =
    {
        "Weapon",
        "Armor",
        "Artifact/Ring",
        "Artifact/Ring"
    };

    private PrototypePlayerStats stats;
    private PrototypeInventory inventory;
    private PrototypeHunger hunger;
    private PrototypePlayerInteraction interaction;
    public static bool IsInventoryOpen { get; private set; }

    public void Initialize(GameObject player)
    {
        stats = player.GetComponent<PrototypePlayerStats>();
        inventory = player.GetComponent<PrototypeInventory>();
        hunger = player.GetComponent<PrototypeHunger>();
        interaction = player.GetComponent<PrototypePlayerInteraction>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            IsInventoryOpen = !IsInventoryOpen;
        }

        if (stats != null && stats.IsDead && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            IsInventoryOpen = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void OnGUI()
    {
        if (stats == null)
        {
            return;
        }

        var oldColor = GUI.color;
        GUI.color = Color.white;
        GUI.Box(new Rect(16f, 16f, 240f, 72f), string.Empty);
        GUI.Label(new Rect(28f, 24f, 210f, 24f), $"Health: {stats.CurrentHealth}/{stats.MaxHealth}");
        GUI.Label(new Rect(28f, 50f, 210f, 24f), $"Hunger: {hunger?.State.ToString() ?? "Satiated"}");

        if (interaction?.CurrentTarget != null)
        {
            var prompt = interaction.CurrentTarget.InteractionPrompt;
            GUI.Box(new Rect(Screen.width * 0.5f - 170f, Screen.height - 72f, 340f, 36f), prompt);
        }

        GUI.Label(new Rect(Screen.width - 190f, 16f, 170f, 24f), "I: Inventory");

        if (stats.IsDead)
        {
            GUI.Box(new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f - 70f, 360f, 140f), "You Died");
            GUI.Label(new Rect(Screen.width * 0.5f - 140f, Screen.height * 0.5f - 20f, 280f, 28f), "Press R to restart LevelTest");
            GUI.color = oldColor;
            return;
        }

        if (IsInventoryOpen)
        {
            DrawInventory();
        }

        GUI.color = oldColor;
    }

    private void DrawInventory()
    {
        const float cellWidth = 112f;
        const float cellHeight = 64f;
        const float panelWidth = cellWidth * 4f + 32f;
        const float panelHeight = cellHeight * 5f + 64f;
        var panelX = Screen.width * 0.5f - panelWidth * 0.5f;
        var panelY = Screen.height * 0.5f - panelHeight * 0.5f;

        GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "Inventory");
        GUI.Label(new Rect(panelX + 16f, panelY + 28f, panelWidth - 32f, 24f), "Top row: equipped");

        for (var slotIndex = 0; slotIndex < PrototypeInventory.SlotCount; slotIndex++)
        {
            var column = slotIndex % 4;
            var row = slotIndex / 4;
            var x = panelX + 16f + column * cellWidth;
            var y = panelY + 54f + row * cellHeight;
            var item = inventory?.GetSlot(slotIndex);
            var label = item == null
                ? (row == 0 ? EquipmentLabels[column] : $"Slot {slotIndex + 1}")
                : item.ItemId;

            if (GUI.Button(new Rect(x, y, cellWidth - 8f, cellHeight - 8f), label) && item != null)
            {
                UseOrEquipItem(slotIndex, item);
            }
        }
    }

    private void UseOrEquipItem(int slotIndex, PrototypeInventoryItem item)
    {
        if (item.ItemId == "health_potion")
        {
            inventory.RemoveAt(slotIndex);
            stats.Heal(8);
            return;
        }

        if (item.ItemId == "food")
        {
            inventory.RemoveAt(slotIndex);
            hunger.Eat(60f);
            return;
        }

        if (item.EquipmentSlot != PrototypeEquipmentSlot.None && slotIndex >= 4)
        {
            inventory.TryEquip(slotIndex, item.EquipmentSlot);
        }
    }

    private void OnDestroy()
    {
        IsInventoryOpen = false;
    }
}
