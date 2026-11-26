using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Lightweight prototype HUD. It intentionally uses immediate-mode GUI so the
/// gameplay loop can be tested before committing to a final UI art direction.
/// </summary>
[DefaultExecutionOrder(-100)]
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
    private PrototypeCombat combat;
    private PrototypeScrolls scrolls;
    private PrototypeDungeonRun run;
    private int inventoryCursor;
    private int actionCursor;
    private bool itemActionsOpen;
    private string inventoryMessage;
    private int targetingScroll = -1;
    private GUIStyle inventorySlotStyle;
    private GUIStyle descriptionStyle;
    public static bool IsInventoryOpen { get; private set; }
    public static bool IsPauseMenuOpen { get; private set; }
    public static bool IsGamePaused => IsInventoryOpen || IsPauseMenuOpen;

    public void Initialize(GameObject player)
    {
        stats = player.GetComponent<PrototypePlayerStats>();
        inventory = player.GetComponent<PrototypeInventory>();
        hunger = player.GetComponent<PrototypeHunger>();
        interaction = player.GetComponent<PrototypePlayerInteraction>();
        combat = player.GetComponent<PrototypeCombat>();
        scrolls = player.GetComponent<PrototypeScrolls>();
        targetingScroll = -1;
        run = GetComponent<PrototypeDungeonRun>();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsInventoryOpen && targetingScroll >= 0)
            {
                inventoryCursor = targetingScroll;
                targetingScroll = -1;
                inventoryMessage = "Scroll use canceled.";
            }
            else if (IsInventoryOpen && itemActionsOpen)
            {
                itemActionsOpen = false;
                inventoryMessage = null;
            }
            else if (IsPauseMenuOpen)
                ResumeGame();
            else
            {
                IsInventoryOpen = false;
                itemActionsOpen = false;
                IsPauseMenuOpen = true;
                ApplyPause();
            }
        }
        else if (!IsPauseMenuOpen && stats != null && !stats.IsDead && Keyboard.current.iKey.wasPressedThisFrame)
        {
            IsInventoryOpen = !IsInventoryOpen;
            itemActionsOpen = false;
            targetingScroll = -1;
            inventoryMessage = null;
            ApplyPause();
        }
        else if (IsInventoryOpen)
        {
            HandleInventoryInput();
        }

        if (stats != null && stats.IsDead && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            IsInventoryOpen = false;
            IsPauseMenuOpen = false;
            ApplyPause();
            if (run != null) run.Restart();
            else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void HandleInventoryInput()
    {
        var keyboard = Keyboard.current;
        var left = keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame;
        var right = keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame;
        var up = keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame;
        var down = keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame;

        if (itemActionsOpen)
        {
            var count = ActionCount(inventory.GetSlot(inventoryCursor), inventoryCursor);
            if (count > 0)
            {
                if (up || left) actionCursor = Mathf.Max(0, actionCursor - 1);
                if (down || right) actionCursor = Mathf.Min(count - 1, actionCursor + 1);
                if (keyboard.jKey.wasPressedThisFrame) ExecuteItemAction();
            }
            return;
        }

        if (left && inventoryCursor % 4 > 0) inventoryCursor--;
        if (right && inventoryCursor % 4 < 3) inventoryCursor++;
        if (up && inventoryCursor >= 4) inventoryCursor -= 4;
        if (down && inventoryCursor < PrototypeInventory.SlotCount - 4) inventoryCursor += 4;
        if (keyboard.jKey.wasPressedThisFrame && inventory.GetSlot(inventoryCursor) != null)
        {
            if (targetingScroll >= 0) SelectScrollTarget(inventoryCursor);
            else OpenItemActions(inventoryCursor);
        }
    }

    private void OpenItemActions(int index)
    {
        inventoryCursor = index;
        actionCursor = 0;
        itemActionsOpen = true;
        inventoryMessage = null;
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
        if (combat != null && combat.OneHitBowEnabled)
            GUI.Box(new Rect(16f, 96f, 240f, 30f), "One-hit bow cheat ON");

        var currentTarget = interaction != null ? interaction.CurrentTarget : null;
        if (currentTarget != null && currentTarget.isActiveAndEnabled)
        {
            var prompt = currentTarget.InteractionPrompt;
            GUI.Box(new Rect(Screen.width * 0.5f - 170f, Screen.height - 72f, 340f, 36f), prompt);
        }

        GUI.Label(new Rect(Screen.width - 190f, 16f, 170f, 24f), "I: Inventory");
        GUI.Label(new Rect(Screen.width - 190f, 64f, 170f, 24f), "Esc: Pause");
        if (run != null)
        {
            GUI.Label(new Rect(Screen.width - 190f, 40f, 180f, 24f), run.Floor == 5 ? "Floor 5: Goo" : $"Floor {run.Floor}/5");
        }

        if (IsPauseMenuOpen)
        {
            DrawPauseMenu();
            GUI.color = oldColor;
            return;
        }

        if (stats.IsDead)
        {
            GUI.Box(new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f - 70f, 360f, 140f), "You Died");
            GUI.Label(new Rect(Screen.width * 0.5f - 140f, Screen.height * 0.5f - 20f, 280f, 28f), "Press R to restart");
            GUI.color = oldColor;
            return;
        }

        if (IsInventoryOpen)
        {
            DrawInventory();
        }

        GUI.color = oldColor;
    }

    private void DrawPauseMenu()
    {
        const float width = 300f;
        const float height = 190f;
        var x = (Screen.width - width) * 0.5f;
        var y = (Screen.height - height) * 0.5f;
        GUI.Box(new Rect(x, y, width, height), "Paused");
        if (GUI.Button(new Rect(x + 35f, y + 48f, width - 70f, 45f), "Continue")) ResumeGame();
        if (GUI.Button(new Rect(x + 35f, y + 108f, width - 70f, 45f), "Quit to Main Menu"))
        {
            ResumeGame();
            SceneManager.LoadScene("MainMenu");
        }
    }

    private static void ResumeGame()
    {
        IsInventoryOpen = false;
        IsPauseMenuOpen = false;
        ApplyPause();
    }

    private static void ApplyPause()
    {
        Time.timeScale = IsGamePaused ? 0f : 1f;
    }

    private void DrawInventory()
    {
        const float cellWidth = 112f;
        const float cellHeight = 64f;
        const float panelWidth = cellWidth * 4f + 32f;
        const float panelHeight = cellHeight * 5f + 90f;
        var panelX = Screen.width * 0.5f - panelWidth * 0.5f;
        var panelY = Screen.height * 0.5f - panelHeight * 0.5f;

        GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "Inventory");
        GUI.Label(new Rect(panelX + 16f, panelY + 28f, panelWidth - 32f, 24f),
            targetingScroll >= 0 ? "Select a target; Esc cancels" : "Top row: equipped");
        if (inventorySlotStyle == null) inventorySlotStyle = new GUIStyle(GUI.skin.button) { wordWrap = true };

        var wasEnabled = GUI.enabled;
        GUI.enabled = wasEnabled && !itemActionsOpen;
        for (var slotIndex = 0; slotIndex < PrototypeInventory.SlotCount; slotIndex++)
        {
            var column = slotIndex % 4;
            var row = slotIndex / 4;
            var x = panelX + 16f + column * cellWidth;
            var y = panelY + 54f + row * cellHeight;
            var item = inventory?.GetSlot(slotIndex);
            var label = item == null
                ? (row == 0 ? EquipmentLabels[column] : $"Slot {slotIndex + 1}")
                : scrolls != null ? scrolls.DisplayName(item) : item.ItemId.Replace('_', ' ');

            var previousBackground = GUI.backgroundColor;
            if (slotIndex == inventoryCursor) GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
            if (GUI.Button(new Rect(x, y, cellWidth - 8f, cellHeight - 8f), label, inventorySlotStyle) && item != null && !itemActionsOpen)
            {
                if (targetingScroll >= 0) SelectScrollTarget(slotIndex);
                else OpenItemActions(slotIndex);
            }
            GUI.backgroundColor = previousBackground;
        }
        GUI.enabled = wasEnabled;

        GUI.Label(new Rect(panelX + 16f, panelY + panelHeight - 30f, panelWidth - 32f, 24f),
            inventoryMessage ?? "WASD / arrows: move   J: select   Esc: back");
        if (itemActionsOpen) DrawItemActions();
    }

    private static int ActionCount(PrototypeInventoryItem item, int index)
    {
        if (item == null) return 0;
        if (index < 4) return 1;
        if (item.ItemId == "health_potion" || item.ItemId == "food" || PrototypeScrolls.IsScroll(item.ItemId)) return 1;
        if (item.EquipmentSlot == PrototypeEquipmentSlot.ArtifactOrRingA || item.EquipmentSlot == PrototypeEquipmentSlot.ArtifactOrRingB) return 2;
        return item.EquipmentSlot == PrototypeEquipmentSlot.None ? 0 : 1;
    }

    private void DrawItemActions()
    {
        var item = inventory.GetSlot(inventoryCursor);
        if (item == null) { itemActionsOpen = false; return; }
        var count = ActionCount(item, inventoryCursor);
        const float width = 290f;
        const float height = 185f;
        var x = (Screen.width - width) * 0.5f;
        var y = (Screen.height - height) * 0.5f;
        var oldColor = GUI.color;
        GUI.color = new Color(0.12f, 0.12f, 0.14f, 1f);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        GUI.color = oldColor;
        GUI.Box(new Rect(x, y, width, height), scrolls != null ? scrolls.DisplayName(item) : item.ItemId.Replace('_', ' '));
        if (count == 0)
            GUI.Label(new Rect(x + 20f, y + 40f, width - 40f, 25f), item.ItemId == "ranger_bow" ? "Always available with J in gameplay." : "No actions available.");
        if (scrolls != null && PrototypeScrolls.IsScroll(item.ItemId))
        {
            if (descriptionStyle == null) descriptionStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            GUI.Label(new Rect(x + 20f, y + 80f, width - 40f, 62f), scrolls.Description(item.ItemId), descriptionStyle);
        }
        if (PrototypeMeleeWeapons.TryGet(item.ItemId, out var weapon))
            GUI.Label(new Rect(x + 20f, y + 112f, width - 40f, 20f), $"Damage {weapon.Damage + item.UpgradeLevel}   Reach {weapon.Range:0.0}   Delay {weapon.Cooldown:0.00}s");
        for (var i = 0; i < count; i++)
        {
            var label = inventoryCursor < 4 ? "Unequip" : item.ItemId == "health_potion" ? "Consume" : item.ItemId == "food" ? "Eat" : PrototypeScrolls.IsScroll(item.ItemId) ? "Read" : count == 2 ? $"Equip: Artifact/Ring {i + 1}" : "Equip";
            var oldBackground = GUI.backgroundColor;
            if (i == actionCursor) GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
            if (GUI.Button(new Rect(x + 25f, y + 38f + i * 39f, width - 50f, 34f), label))
            {
                actionCursor = i;
                ExecuteItemAction();
            }
            GUI.backgroundColor = oldBackground;
        }
        GUI.Label(new Rect(x + 20f, y + 150f, width - 40f, 20f), inventoryMessage ?? "J: confirm    Esc: deselect");
    }

    private void ExecuteItemAction()
    {
        var item = inventory.GetSlot(inventoryCursor);
        if (item == null) { itemActionsOpen = false; return; }
        if (inventoryCursor < 4)
        {
            if (!inventory.TryUnequip(inventoryCursor)) { inventoryMessage = item.IsCursed ? "Cursed equipment cannot be removed." : "Inventory is full."; return; }
        }
        else if (item.ItemId == "health_potion")
        {
            if (stats.CurrentHealth >= stats.MaxHealth) { inventoryMessage = "Health is already full."; return; }
            inventory.RemoveAt(inventoryCursor);
            stats.Heal(8);
        }
        else if (item.ItemId == "food")
        {
            inventory.RemoveAt(inventoryCursor);
            hunger.Eat(60f);
        }
        else if (PrototypeScrolls.IsScroll(item.ItemId))
        {
            if (scrolls == null || !scrolls.Read(inventoryCursor, out var selectTarget, out inventoryMessage)) return;
            itemActionsOpen = false;
            if (selectTarget) targetingScroll = inventoryCursor;
            return;
        }
        else
        {
            var slot = item.EquipmentSlot;
            if (slot == PrototypeEquipmentSlot.ArtifactOrRingA || slot == PrototypeEquipmentSlot.ArtifactOrRingB)
                slot = actionCursor == 0 ? PrototypeEquipmentSlot.ArtifactOrRingA : PrototypeEquipmentSlot.ArtifactOrRingB;
            if (!inventory.TryEquip(inventoryCursor, slot)) { inventoryMessage = "Cannot equip: slot may be cursed."; return; }
        }
        itemActionsOpen = false;
        inventoryMessage = null;
    }

    private void SelectScrollTarget(int index)
    {
        if (scrolls == null || targetingScroll < 0) return;
        inventoryCursor = index;
        if (!scrolls.ReadOnTarget(targetingScroll, index, out inventoryMessage)) return;
        targetingScroll = -1;
    }

    private void OnDestroy()
    {
        IsInventoryOpen = false;
        IsPauseMenuOpen = false;
        Time.timeScale = 1f;
    }
}
