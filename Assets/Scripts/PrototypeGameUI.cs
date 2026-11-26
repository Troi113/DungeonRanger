using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Prototype HUD, inventory, and pause menu. Uses immediate-mode GUI drawn with
/// the medieval pixel-art pack through PrototypeUITheme.
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
            var count = ActionsFor(inventory.GetSlot(inventoryCursor), inventoryCursor).Count;
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

        PrototypeUITheme.BeginScaledGui();
        DrawHud();

        var currentTarget = interaction != null ? interaction.CurrentTarget : null;
        if (currentTarget != null && currentTarget.isActiveAndEnabled && !IsGamePaused && !stats.IsDead)
            DrawPrompt(currentTarget.InteractionPrompt);

        if (IsPauseMenuOpen)
        {
            DrawPauseMenu();
            return;
        }

        if (stats.IsDead)
        {
            DrawDeathScreen();
            return;
        }

        if (IsInventoryOpen)
        {
            DrawInventory();
        }
    }

    private void DrawHud()
    {
        var left = new Rect(16f, 16f, 290f, 100f);
        PrototypeUITheme.DrawPanel(left);
        var healthFraction = stats.MaxHealth > 0 ? (float)stats.CurrentHealth / stats.MaxHealth : 0f;
        PrototypeUITheme.DrawIcon(new Rect(left.x + 18f, left.y + 16f, 32f, 32f),
            healthFraction <= 0.3f ? PrototypeUITheme.BrokenHeart : PrototypeUITheme.Heart);
        var bar = new Rect(left.x + 58f, left.y + 20f, left.width - 76f, 24f);
        PrototypeUITheme.DrawBar(bar, healthFraction, new Color(0.78f, 0.12f, 0.1f));
        PrototypeUITheme.Label(bar, $"{stats.CurrentHealth} / {stats.MaxHealth}", 14, PrototypeUITheme.Parchment, TextAnchor.MiddleCenter, true);

        var hungerState = hunger != null ? hunger.State : PrototypeHungerState.Satiated;
        var hungerColor = hungerState == PrototypeHungerState.Famished ? PrototypeUITheme.Danger
            : hungerState == PrototypeHungerState.Hungry ? PrototypeUITheme.Warning : PrototypeUITheme.Parchment;
        PrototypeUITheme.DrawIcon(new Rect(left.x + 18f, left.y + 54f, 32f, 32f), PrototypeUITheme.Bread);
        PrototypeUITheme.Label(new Rect(left.x + 58f, left.y + 54f, left.width - 76f, 32f), hungerState.ToString(), 16, hungerColor, TextAnchor.MiddleLeft, true);

        if (combat != null && combat.OneHitBowEnabled)
        {
            var cheat = new Rect(16f, left.yMax + 6f, 290f, 48f);
            PrototypeUITheme.DrawPanel(cheat);
            PrototypeUITheme.DrawIcon(new Rect(cheat.x + 16f, cheat.y + 10f, 28f, 28f), PrototypeUITheme.Star);
            PrototypeUITheme.Label(new Rect(cheat.x + 52f, cheat.y, cheat.width - 64f, cheat.height), "One-hit bow cheat ON", 14, PrototypeUITheme.Gold, TextAnchor.MiddleLeft, true);
        }

        var right = new Rect(PrototypeUITheme.Width - 246f, 16f, 230f, 100f);
        PrototypeUITheme.DrawPanel(right);
        if (run != null)
        {
            var bossFloor = run.Floor == 5;
            PrototypeUITheme.DrawIcon(new Rect(right.x + 18f, right.y + 14f, 32f, 32f), bossFloor ? PrototypeUITheme.Skull : PrototypeUITheme.Castle);
            PrototypeUITheme.Label(new Rect(right.x + 58f, right.y + 14f, right.width - 74f, 32f),
                bossFloor ? "Floor 5: Goo" : $"Floor {run.Floor} / 5", 16, bossFloor ? PrototypeUITheme.Danger : PrototypeUITheme.Gold, TextAnchor.MiddleLeft, true);
        }
        PrototypeUITheme.DrawIcon(new Rect(right.x + 20f, right.y + 58f, 24f, 24f), PrototypeUITheme.Bag);
        PrototypeUITheme.Label(new Rect(right.x + 48f, right.y + 56f, 70f, 28f), "I  Bag", 13, PrototypeUITheme.Muted);
        PrototypeUITheme.DrawIcon(new Rect(right.x + 120f, right.y + 58f, 24f, 24f), PrototypeUITheme.Pause);
        PrototypeUITheme.Label(new Rect(right.x + 148f, right.y + 56f, 70f, 28f), "Esc  Pause", 13, PrototypeUITheme.Muted);
    }

    private static void DrawPrompt(string prompt)
    {
        var rect = new Rect(PrototypeUITheme.Width * 0.5f - 200f, PrototypeUITheme.Height - 86f, 400f, 54f);
        PrototypeUITheme.DrawPanel(rect);
        PrototypeUITheme.Label(rect, prompt, 16, PrototypeUITheme.Parchment, TextAnchor.MiddleCenter, true);
    }

    private static void DrawDeathScreen()
    {
        PrototypeUITheme.Fill(new Rect(0f, 0f, PrototypeUITheme.Width, PrototypeUITheme.Height), new Color(0.15f, 0f, 0f, 0.55f));
        const float width = 380f;
        const float height = 230f;
        var rect = new Rect((PrototypeUITheme.Width - width) * 0.5f, (PrototypeUITheme.Height - height) * 0.5f, width, height);
        PrototypeUITheme.DrawPanel(rect);
        PrototypeUITheme.DrawIcon(new Rect(rect.center.x - 32f, rect.y + 24f, 64f, 64f), PrototypeUITheme.Skull);
        PrototypeUITheme.Label(new Rect(rect.x, rect.y + 96f, width, 48f), "You Died", 32, PrototypeUITheme.Danger, TextAnchor.MiddleCenter, true);
        PrototypeUITheme.Label(new Rect(rect.x, rect.y + 150f, width, 40f), "Press R to restart", 17, PrototypeUITheme.Parchment, TextAnchor.MiddleCenter);
    }

    private void DrawPauseMenu()
    {
        PrototypeUITheme.Fill(new Rect(0f, 0f, PrototypeUITheme.Width, PrototypeUITheme.Height), new Color(0f, 0f, 0f, 0.55f));
        const float width = 340f;
        const float height = 270f;
        var x = (PrototypeUITheme.Width - width) * 0.5f;
        var y = (PrototypeUITheme.Height - height) * 0.5f;
        PrototypeUITheme.DrawPanel(new Rect(x, y, width, height));
        PrototypeUITheme.DrawIcon(new Rect(x + width * 0.5f - 24f, y + 22f, 48f, 48f), PrototypeUITheme.Pause);
        PrototypeUITheme.Label(new Rect(x, y + 74f, width, 36f), "Paused", 26, PrototypeUITheme.Gold, TextAnchor.MiddleCenter, true);
        if (PrototypeUITheme.Button(new Rect(x + 40f, y + 124f, width - 80f, 48f), "Continue")) ResumeGame();
        if (PrototypeUITheme.Button(new Rect(x + 40f, y + 186f, width - 80f, 48f), "Quit to Main Menu"))
        {
            // Quitting keeps the run as a game in progress (a finished run is deleted instead).
            if (run != null) run.SaveRun();
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
        const float cell = 96f;
        const float panelWidth = cell * 4f + 48f;
        const float panelHeight = cell * 5f + 150f;
        var panelX = PrototypeUITheme.Width * 0.5f - panelWidth * 0.5f;
        var panelY = PrototypeUITheme.Height * 0.5f - panelHeight * 0.5f;

        PrototypeUITheme.Fill(new Rect(0f, 0f, PrototypeUITheme.Width, PrototypeUITheme.Height), new Color(0f, 0f, 0f, 0.45f));
        PrototypeUITheme.DrawPanel(new Rect(panelX, panelY, panelWidth, panelHeight));
        PrototypeUITheme.DrawIcon(new Rect(panelX + 24f, panelY + 20f, 36f, 36f), PrototypeUITheme.Bag);
        PrototypeUITheme.Label(new Rect(panelX + 68f, panelY + 20f, 220f, 36f), "Inventory", 24, PrototypeUITheme.Gold, TextAnchor.MiddleLeft, true);
        PrototypeUITheme.Label(new Rect(panelX + 24f, panelY + 58f, panelWidth - 48f, 22f),
            targetingScroll >= 0 ? "Select a target  -  Esc cancels" : "Top row: equipped", 13,
            targetingScroll >= 0 ? PrototypeUITheme.Gold : PrototypeUITheme.Muted);

        for (var slotIndex = 0; slotIndex < PrototypeInventory.SlotCount; slotIndex++)
        {
            var column = slotIndex % 4;
            var row = slotIndex / 4;
            var rect = new Rect(panelX + 24f + column * cell, panelY + 86f + row * cell, cell - 8f, cell - 8f);
            var item = inventory?.GetSlot(slotIndex);
            PrototypeUITheme.DrawSlot(rect, row == 0, slotIndex == inventoryCursor);
            var iconRect = new Rect(rect.center.x - 22f, rect.y + 10f, 44f, 44f);

            if (item == null)
            {
                if (row == 0)
                {
                    PrototypeUITheme.DrawIcon(iconRect, PrototypeUITheme.EquipmentPlaceholder(column), 0.25f);
                    PrototypeUITheme.Label(new Rect(rect.x + 6f, rect.yMax - 32f, rect.width - 12f, 26f), EquipmentLabels[column], 11, PrototypeUITheme.Muted, TextAnchor.MiddleCenter);
                }
                continue;
            }

            var icon = PrototypeUITheme.ItemIcon(item.ItemId);
            if (icon != null) PrototypeUITheme.DrawIcon(iconRect, icon);
            else PrototypeUITheme.DrawIcon(iconRect, PrototypeUITheme.Exclamation, 0.6f);
            var name = scrolls != null ? scrolls.DisplayName(item) : item.ItemId.Replace('_', ' ');
            var nameColor = item.IsIdentified && item.IsCursed ? PrototypeUITheme.Danger : PrototypeUITheme.Parchment;
            PrototypeUITheme.Label(new Rect(rect.x + 6f, rect.yMax - 34f, rect.width - 12f, 30f), name, 11, nameColor, TextAnchor.MiddleCenter, false, true);

            if (!itemActionsOpen && GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                if (targetingScroll >= 0) SelectScrollTarget(slotIndex);
                else OpenItemActions(slotIndex);
            }
        }

        PrototypeUITheme.Label(new Rect(panelX + 24f, panelY + panelHeight - 52f, panelWidth - 48f, 30f),
            inventoryMessage ?? "WASD / arrows: move    J: select    Esc: back", 13,
            inventoryMessage != null ? PrototypeUITheme.Parchment : PrototypeUITheme.Muted, TextAnchor.MiddleCenter, false, true);
        if (itemActionsOpen) DrawItemActions();
    }

    private enum ItemAction { Unequip, Consume, Eat, Read, Equip, EquipRingA, EquipRingB, Drop, Throw }

    private readonly System.Collections.Generic.List<ItemAction> actionBuffer = new();

    /// <summary>Fills actionBuffer with the actions available for the item in a slot.</summary>
    private System.Collections.Generic.List<ItemAction> ActionsFor(PrototypeInventoryItem item, int index)
    {
        actionBuffer.Clear();
        if (item == null) return actionBuffer;
        if (index < 4) actionBuffer.Add(ItemAction.Unequip);
        else if (item.ItemId == "health_potion") actionBuffer.Add(ItemAction.Consume);
        else if (item.ItemId == "food") actionBuffer.Add(ItemAction.Eat);
        else if (PrototypeScrolls.IsScroll(item.ItemId)) actionBuffer.Add(ItemAction.Read);
        else if (item.EquipmentSlot == PrototypeEquipmentSlot.ArtifactOrRingA || item.EquipmentSlot == PrototypeEquipmentSlot.ArtifactOrRingB)
        {
            actionBuffer.Add(ItemAction.EquipRingA);
            actionBuffer.Add(ItemAction.EquipRingB);
        }
        else if (item.EquipmentSlot != PrototypeEquipmentSlot.None) actionBuffer.Add(ItemAction.Equip);

        // Cursed equipment that is being worn cannot leave the ranger.
        var stuck = index < 4 && item.IsCursed;
        if (PrototypeItemCatalog.CanLeaveInventory(item.ItemId) && !stuck)
        {
            actionBuffer.Add(ItemAction.Drop);
            actionBuffer.Add(ItemAction.Throw);
        }
        return actionBuffer;
    }

    private static string ActionLabel(ItemAction action) => action switch
    {
        ItemAction.Unequip => "Unequip",
        ItemAction.Consume => "Consume",
        ItemAction.Eat => "Eat",
        ItemAction.Read => "Read",
        ItemAction.Equip => "Equip",
        ItemAction.EquipRingA => "Equip: Artifact/Ring 1",
        ItemAction.EquipRingB => "Equip: Artifact/Ring 2",
        ItemAction.Drop => "Drop",
        ItemAction.Throw => "Throw",
        _ => action.ToString()
    };

    private void ExecuteItemAction()
    {
        var item = inventory.GetSlot(inventoryCursor);
        if (item == null) { itemActionsOpen = false; return; }
        var actions = ActionsFor(item, inventoryCursor);
        if (actionCursor < 0 || actionCursor >= actions.Count) return;

        switch (actions[actionCursor])
        {
            case ItemAction.Unequip:
                if (!inventory.TryUnequip(inventoryCursor)) { inventoryMessage = item.IsCursed ? "Cursed equipment cannot be removed." : "Inventory is full."; return; }
                break;
            case ItemAction.Consume:
                if (stats.CurrentHealth >= stats.MaxHealth) { inventoryMessage = "Health is already full."; return; }
                inventory.RemoveAt(inventoryCursor);
                stats.Heal(8);
                break;
            case ItemAction.Eat:
                inventory.RemoveAt(inventoryCursor);
                hunger.Eat(60f);
                break;
            case ItemAction.Read:
                if (scrolls == null || !scrolls.Read(inventoryCursor, out var selectTarget, out inventoryMessage)) return;
                itemActionsOpen = false;
                if (selectTarget) targetingScroll = inventoryCursor;
                return;
            case ItemAction.Equip:
            case ItemAction.EquipRingA:
            case ItemAction.EquipRingB:
            {
                var slot = actions[actionCursor] == ItemAction.EquipRingA ? PrototypeEquipmentSlot.ArtifactOrRingA
                    : actions[actionCursor] == ItemAction.EquipRingB ? PrototypeEquipmentSlot.ArtifactOrRingB
                    : item.EquipmentSlot;
                if (!inventory.TryEquip(inventoryCursor, slot)) { inventoryMessage = "Cannot equip: slot may be cursed."; return; }
                itemActionsOpen = false;
                inventoryMessage = item.IsCursed ? $"The {item.ItemId.Replace('_', ' ')} is cursed and binds to you!" : null;
                return;
            }
            case ItemAction.Drop:
            {
                // Drops the whole stack where the ranger stands.
                var dropped = inventory.RemoveAt(inventoryCursor);
                var position = stats.transform.position;
                var pickup = PrototypeItemPickup.CreateForItem(FloorParent(), new Vector3(position.x, 0f, position.z), dropped);
                PrototypeDungeonRun.HoldDoorOpenIfInDoorway(FloorParent(), pickup);
                inventoryMessage = $"Dropped {DisplayName(dropped)}.";
                itemActionsOpen = false;
                return;
            }
            case ItemAction.Throw:
            {
                // Throws one item from a stack, then closes the inventory so play resumes.
                var thrown = inventory.TakeOneAt(inventoryCursor);
                itemActionsOpen = false;
                inventoryMessage = null;
                IsInventoryOpen = false;
                ApplyPause();
                PrototypeThrownItem.Throw(thrown, stats.GetComponent<PrototypePlayerMovement>(), FloorParent());
                return;
            }
        }
        itemActionsOpen = false;
        inventoryMessage = null;
    }

    private Transform FloorParent() => run != null ? run.CurrentFloorRoot : stats.transform.parent;

    private string DisplayName(PrototypeInventoryItem item) =>
        scrolls != null ? scrolls.DisplayName(item) : item.ItemId.Replace('_', ' ');

    private void DrawItemActions()
    {
        var item = inventory.GetSlot(inventoryCursor);
        if (item == null) { itemActionsOpen = false; return; }
        var actions = ActionsFor(item, inventoryCursor);
        var count = actions.Count;
        const float width = 360f;
        var height = 190f + Mathf.Max(1, count) * 50f;
        var x = (PrototypeUITheme.Width - width) * 0.5f;
        var y = (PrototypeUITheme.Height - height) * 0.5f;

        PrototypeUITheme.Fill(new Rect(0f, 0f, PrototypeUITheme.Width, PrototypeUITheme.Height), new Color(0f, 0f, 0f, 0.35f));
        PrototypeUITheme.DrawPanel(new Rect(x, y, width, height));
        PrototypeUITheme.DrawIcon(new Rect(x + 24f, y + 22f, 44f, 44f), PrototypeUITheme.ItemIcon(item.ItemId));
        PrototypeUITheme.Label(new Rect(x + 78f, y + 22f, width - 102f, 44f), DisplayName(item), 18, PrototypeUITheme.Gold, TextAnchor.MiddleLeft, true, true);

        string info = null;
        if (item.ItemId == "ranger_bow") info = "Always available with J in gameplay.";
        else if (scrolls != null && PrototypeScrolls.IsScroll(item.ItemId)) info = scrolls.Description(item.ItemId);
        else if (PrototypeMeleeWeapons.TryGet(item.ItemId, out var weapon))
            info = $"Damage {weapon.Damage + item.UpgradeLevel}    Reach {weapon.Range:0} tile{(weapon.Range > 1f ? "s" : "")}    Delay {weapon.Cooldown:0.00}s";
        else if (PrototypeArmors.TryGet(item.ItemId, out _))
            info = $"Absorbs 0-{PrototypeArmors.MaxAbsorb(item)} damage per enemy hit";
        else if (item.ItemId == "health_potion") info = "Restores 8 health.";
        else if (item.ItemId == "food") info = "Restores hunger.";
        if (count == 0 && info == null) info = "No actions available.";
        PrototypeUITheme.Label(new Rect(x + 24f, y + 76f, width - 48f, 56f), info, 13, PrototypeUITheme.Parchment, TextAnchor.UpperLeft, false, true);

        for (var i = 0; i < count; i++)
        {
            if (PrototypeUITheme.Button(new Rect(x + 32f, y + 138f + i * 50f, width - 64f, 42f), ActionLabel(actions[i]), i == actionCursor))
            {
                actionCursor = i;
                ExecuteItemAction();
            }
        }
        PrototypeUITheme.Label(new Rect(x + 24f, y + height - 46f, width - 48f, 28f),
            inventoryMessage ?? "J: confirm    Esc: back", 13,
            inventoryMessage != null ? PrototypeUITheme.Warning : PrototypeUITheme.Muted, TextAnchor.MiddleCenter, false, true);
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
