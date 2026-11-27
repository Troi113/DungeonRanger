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
    private static string noticeText;
    private static float noticeUntil;

    /// <summary>Shows a short message above the interaction prompt (uses unscaled time).</summary>
    public static void ShowNotice(string text, float seconds = 2.5f)
    {
        noticeText = text;
        noticeUntil = Time.unscaledTime + seconds;
    }

    public static bool IsInventoryOpen { get; private set; }
    public static bool IsPauseMenuOpen { get; private set; }
    public static bool IsTalentsOpen { get; private set; }
    public static bool IsGamePaused => IsInventoryOpen || IsPauseMenuOpen || IsTalentsOpen;
    private int talentCursor;

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

    // Hidden combat log: shown/hidden by pressing E five times in a row.
    private const int CombatLogPresses = 5;
    private const int CombatLogLines = 12;
    private int combatLogKeyCount;
    private bool showCombatLog;

    private void HandleCombatLogToggle(Keyboard keyboard)
    {
        if (!keyboard.anyKey.wasPressedThisFrame) return;
        if (!keyboard.eKey.wasPressedThisFrame)
        {
            combatLogKeyCount = 0;
            return;
        }
        if (++combatLogKeyCount < CombatLogPresses) return;
        combatLogKeyCount = 0;
        showCombatLog = !showCombatLog;
        ShowNotice(showCombatLog ? "Combat log shown." : "Combat log hidden.");
    }

    private void DrawCombatLog()
    {
        var entries = PrototypeCombatLog.Entries;
        const float lineHeight = 18f;
        var count = Mathf.Min(CombatLogLines, entries.Count);
        var rect = new Rect(PrototypeUITheme.Width - 436f, PrototypeUITheme.Height - 72f - lineHeight * CombatLogLines, 420f, lineHeight * CombatLogLines + 52f);
        PrototypeUITheme.DrawPanel(rect);
        PrototypeUITheme.Label(new Rect(rect.x + 16f, rect.y + 8f, rect.width - 32f, 22f), "Combat Log", 14, PrototypeUITheme.Gold, TextAnchor.MiddleLeft, true);
        if (count == 0)
            PrototypeUITheme.Label(new Rect(rect.x + 16f, rect.y + 34f, rect.width - 32f, lineHeight), "Nothing yet.", 12, PrototypeUITheme.Muted);
        for (var i = 0; i < count; i++)
        {
            var text = entries[entries.Count - count + i];
            PrototypeUITheme.Label(new Rect(rect.x + 16f, rect.y + 34f + i * lineHeight, rect.width - 32f, lineHeight), text, 12,
                i == count - 1 ? PrototypeUITheme.Parchment : PrototypeUITheme.Muted);
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        HandleCombatLogToggle(Keyboard.current);

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsTalentsOpen)
            {
                IsTalentsOpen = false;
                ApplyPause();
            }
            else if (IsInventoryOpen && targetingScroll >= 0)
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
            else if (IsInventoryOpen)
            {
                // Esc on the inventory grid closes it and resumes play.
                IsInventoryOpen = false;
                inventoryMessage = null;
                ApplyPause();
            }
            else
            {
                IsInventoryOpen = false;
                itemActionsOpen = false;
                IsPauseMenuOpen = true;
                ApplyPause();
            }
        }
        else if (!IsPauseMenuOpen && !IsInventoryOpen && stats != null && !stats.IsDead && Keyboard.current.oKey.wasPressedThisFrame)
        {
            IsTalentsOpen = !IsTalentsOpen;
            ApplyPause();
        }
        else if (IsTalentsOpen)
        {
            HandleTalentInput();
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
            RestartRun();
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
        if (showCombatLog) DrawCombatLog();

        var currentTarget = interaction != null ? interaction.CurrentTarget : null;
        var searchProgress = interaction != null ? interaction.SearchProgress : 0f;
        var searchMessage = interaction != null ? interaction.SearchMessage : null;
        if (!IsGamePaused && !stats.IsDead)
        {
            if (searchProgress > 0f) DrawSearchProgress(searchProgress);
            else if (noticeText != null && Time.unscaledTime < noticeUntil) DrawPrompt(noticeText);
            else if (searchMessage != null) DrawPrompt(searchMessage);
            else if (currentTarget != null && currentTarget.isActiveAndEnabled) DrawPrompt(currentTarget.InteractionPrompt);
        }

        if (IsPauseMenuOpen)
        {
            DrawPauseMenu();
            return;
        }

        if (stats.IsDead)
        {
            // Red flash right away; the game-over screen waits for the fall, then fades in.
            var sinceDeath = PrototypeHeroDeath.DeathTime >= 0f ? Time.unscaledTime - PrototypeHeroDeath.DeathTime : 99f;
            var flash = Mathf.Clamp01(1f - sinceDeath / 0.6f) * 0.45f;
            if (flash > 0f) PrototypeUITheme.Fill(new Rect(0f, 0f, PrototypeUITheme.Width, PrototypeUITheme.Height), new Color(0.8f, 0f, 0f, flash));
            var reveal = Mathf.Clamp01((sinceDeath - PrototypeHeroDeath.ScreenDelay) / 0.6f);
            if (reveal > 0f)
            {
                PrototypeUITheme.GlobalAlpha = reveal;
                DrawDeathScreen();
                PrototypeUITheme.GlobalAlpha = 1f;
            }
            return;
        }

        if (IsTalentsOpen)
        {
            DrawTalents();
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
        var hungerColor = hungerState == PrototypeHungerState.Starving ? PrototypeUITheme.Danger
            : hungerState == PrototypeHungerState.Hungry ? PrototypeUITheme.Warning : PrototypeUITheme.Parchment;
        PrototypeUITheme.DrawIcon(new Rect(left.x + 18f, left.y + 54f, 32f, 32f), PrototypeUITheme.Bread);
        PrototypeUITheme.Label(new Rect(left.x + 58f, left.y + 54f, left.width - 76f, 32f), hungerState.ToString(), 16, hungerColor, TextAnchor.MiddleLeft, true);
        PrototypeUITheme.Label(new Rect(left.x + 58f, left.y + 54f, left.width - 76f, 32f), $"Lv {stats.Level}   STR {stats.Strength}", 16, PrototypeUITheme.Gold, TextAnchor.MiddleRight, true);
        // Thin experience bar under the panel.
        PrototypeUITheme.DrawBar(new Rect(left.x + 12f, left.yMax - 10f, left.width - 24f, 8f),
            stats.ExperienceToLevel > 0 ? (float)stats.Experience / stats.ExperienceToLevel : 0f, new Color(0.35f, 0.75f, 1f));

        var buffsBottom = DrawBuffIndicator(left.yMax + 6f);
        if (combat != null && combat.OneHitBowEnabled)
        {
            var cheat = new Rect(16f, buffsBottom, 290f, 48f);
            PrototypeUITheme.DrawPanel(cheat);
            PrototypeUITheme.DrawIcon(new Rect(cheat.x + 16f, cheat.y + 10f, 28f, 28f), PrototypeUITheme.Star);
            PrototypeUITheme.Label(new Rect(cheat.x + 52f, cheat.y, cheat.width - 64f, cheat.height), "One-hit bow cheat ON", 14, PrototypeUITheme.Gold, TextAnchor.MiddleLeft, true);
        }

        var right = new Rect(PrototypeUITheme.Width - 246f, 16f, 230f, 130f);
        PrototypeUITheme.DrawPanel(right);
        if (run != null)
        {
            var bossFloor = run.Floor == 5;
            PrototypeUITheme.DrawIcon(new Rect(right.x + 18f, right.y + 14f, 32f, 32f), bossFloor ? PrototypeUITheme.Skull : PrototypeUITheme.Castle);
            PrototypeUITheme.Label(new Rect(right.x + 58f, right.y + 14f, right.width - 74f, 32f),
                bossFloor ? "Floor 5: Goo" : $"Floor {run.Floor} / 5", 16, bossFloor ? PrototypeUITheme.Danger : PrototypeUITheme.Gold, TextAnchor.MiddleLeft, true);
        }
        if (run != null)
        {
            PrototypeUITheme.DrawIcon(new Rect(right.x + 20f, right.y + 50f, 24f, 24f), PrototypeUITheme.Star);
            PrototypeUITheme.Label(new Rect(right.x + 48f, right.y + 48f, 90f, 28f), run.Score.ToString(), 15, PrototypeUITheme.Gold, TextAnchor.MiddleLeft, true);
            PrototypeUITheme.Label(new Rect(right.x + 130f, right.y + 48f, 80f, 28f), PrototypeLeaderboard.FormatTime(run.ElapsedSeconds), 15, PrototypeUITheme.Parchment, TextAnchor.MiddleRight, true);
        }
        PrototypeUITheme.DrawIcon(new Rect(right.x + 20f, right.y + 88f, 24f, 24f), PrototypeUITheme.Bag);
        PrototypeUITheme.Label(new Rect(right.x + 48f, right.y + 86f, 70f, 28f), "I  Bag", 13, PrototypeUITheme.Muted);
        PrototypeUITheme.DrawIcon(new Rect(right.x + 120f, right.y + 88f, 24f, 24f), PrototypeUITheme.Pause);
        PrototypeUITheme.Label(new Rect(right.x + 148f, right.y + 86f, 70f, 28f), "Esc  Pause", 13, PrototypeUITheme.Muted);
    }

    private readonly System.Collections.Generic.List<PrototypeStatusEffectType> activeBuffs = new();

    /// <summary>
    /// Pixel Dungeon's buff indicator: one small tag per active buff or debuff (and the Hungry /
    /// Starving hunger debuffs) with its time left. Returns the y below the row.
    /// </summary>
    private float DrawBuffIndicator(float y)
    {
        var statuses = stats.GetComponent<PrototypeStatusEffects>();
        activeBuffs.Clear();
        statuses?.GetActive(activeBuffs);
        var hungerState = hunger != null ? hunger.State : PrototypeHungerState.Satiated;
        var count = activeBuffs.Count + (hungerState != PrototypeHungerState.Satiated ? 1 : 0);
        if (count == 0) return y;

        const float tagWidth = 138f;
        const float tagHeight = 30f;
        var x = 16f;
        var rowY = y;
        void Tag(Texture2D icon, string text, Color color)
        {
            if (x + tagWidth > 16f + tagWidth * 2f + 4f) { x = 16f; rowY += tagHeight + 4f; }
            var rect = new Rect(x, rowY, tagWidth, tagHeight);
            PrototypeUITheme.Fill(rect, new Color(0.08f, 0.05f, 0.04f, 0.8f));
            PrototypeUITheme.DrawIcon(new Rect(rect.x + 4f, rect.y + 3f, 24f, 24f), icon);
            PrototypeUITheme.Label(new Rect(rect.x + 32f, rect.y, rect.width - 36f, rect.height), text, 13, color, TextAnchor.MiddleLeft, true);
            x += tagWidth + 4f;
        }

        if (hungerState == PrototypeHungerState.Starving) Tag(PrototypeUITheme.Skull, "Starving", PrototypeUITheme.Danger);
        else if (hungerState == PrototypeHungerState.Hungry) Tag(PrototypeUITheme.Bread, "Hungry", PrototypeUITheme.Warning);
        foreach (var type in activeBuffs)
        {
            var remaining = statuses.Remaining(type);
            var debuff = type != PrototypeStatusEffectType.Invisible && type != PrototypeStatusEffectType.Barkskin;
            var label = type == PrototypeStatusEffectType.Bleeding ? $"{BuffName(type)} {remaining:0}" : $"{BuffName(type)} {Mathf.CeilToInt(remaining)}s";
            Tag(BuffIcon(type), label, debuff ? PrototypeUITheme.Danger : new Color(0.55f, 0.85f, 1f));
        }
        return rowY + tagHeight + 6f;
    }

    private static string BuffName(PrototypeStatusEffectType type) => type switch
    {
        PrototypeStatusEffectType.Poisoned => "Poisoned",
        PrototypeStatusEffectType.Toxic => "Toxic",
        PrototypeStatusEffectType.Paralyzed => "Paralysed",
        PrototypeStatusEffectType.Gripped => "Rooted",
        PrototypeStatusEffectType.Bleeding => "Bleeding",
        PrototypeStatusEffectType.Oozed => "Ooze",
        PrototypeStatusEffectType.Slowed => "Slowed",
        PrototypeStatusEffectType.Invisible => "Invisible",
        PrototypeStatusEffectType.Burning => "Burning",
        PrototypeStatusEffectType.Barkskin => "Barkskin",
        PrototypeStatusEffectType.Crippled => "Crippled",
        _ => type.ToString()
    };

    private static Texture2D BuffIcon(PrototypeStatusEffectType type) => type switch
    {
        PrototypeStatusEffectType.Burning => PrototypeUITheme.Icon(3, 22),
        PrototypeStatusEffectType.Bleeding => PrototypeUITheme.BrokenHeart,
        PrototypeStatusEffectType.Poisoned or PrototypeStatusEffectType.Toxic or PrototypeStatusEffectType.Oozed => PrototypeUITheme.Icon(3, 19),
        PrototypeStatusEffectType.Paralyzed => PrototypeUITheme.Icon(3, 24),
        PrototypeStatusEffectType.Slowed or PrototypeStatusEffectType.Crippled => PrototypeUITheme.Icon(3, 59),
        PrototypeStatusEffectType.Invisible => PrototypeUITheme.Icon(3, 36),
        PrototypeStatusEffectType.Barkskin => PrototypeUITheme.Icon(3, 44),
        PrototypeStatusEffectType.Gripped => PrototypeUITheme.Icon(3, 31),
        _ => PrototypeUITheme.Exclamation
    };

    private static void DrawPrompt(string prompt)
    {
        var rect = new Rect(PrototypeUITheme.Width * 0.5f - 200f, PrototypeUITheme.Height - 86f, 400f, 54f);
        PrototypeUITheme.DrawPanel(rect);
        PrototypeUITheme.Label(rect, prompt, 16, PrototypeUITheme.Parchment, TextAnchor.MiddleCenter, true);
    }

    private static void DrawSearchProgress(float progress)
    {
        var rect = new Rect(PrototypeUITheme.Width * 0.5f - 200f, PrototypeUITheme.Height - 96f, 400f, 64f);
        PrototypeUITheme.DrawPanel(rect);
        PrototypeUITheme.Label(new Rect(rect.x, rect.y + 8f, rect.width, 24f), "Searching...", 15, PrototypeUITheme.Parchment, TextAnchor.MiddleCenter, true);
        PrototypeUITheme.DrawBar(new Rect(rect.x + 40f, rect.y + 36f, rect.width - 80f, 14f), progress, PrototypeUITheme.Gold);
    }

    private void DrawDeathScreen()
    {
        PrototypeUITheme.Fill(new Rect(0f, 0f, PrototypeUITheme.Width, PrototypeUITheme.Height), new Color(0.15f, 0f, 0f, 0.55f));
        const float width = 420f;
        const float height = 380f;
        var rect = new Rect((PrototypeUITheme.Width - width) * 0.5f, (PrototypeUITheme.Height - height) * 0.5f, width, height);
        PrototypeUITheme.DrawPanel(rect);
        PrototypeUITheme.DrawIcon(new Rect(rect.center.x - 32f, rect.y + 22f, 64f, 64f), PrototypeUITheme.Skull);
        PrototypeUITheme.Label(new Rect(rect.x, rect.y + 90f, width, 44f), "Game Over", 32, PrototypeUITheme.Danger, TextAnchor.MiddleCenter, true);

        var result = PrototypeLeaderboard.LastRun;
        if (result != null)
        {
            PrototypeUITheme.Label(new Rect(rect.x, rect.y + 140f, width, 28f), $"Score  {result.Score}", 20, PrototypeUITheme.Gold, TextAnchor.MiddleCenter, true);
            PrototypeUITheme.Label(new Rect(rect.x, rect.y + 170f, width, 24f),
                $"Reached floor {result.Floor}    Time {PrototypeLeaderboard.FormatTime(result.Seconds)}", 15, PrototypeUITheme.Parchment, TextAnchor.MiddleCenter);
            PrototypeUITheme.Label(new Rect(rect.x, rect.y + 198f, width, 24f),
                PrototypeLeaderboard.LastRunWasBest ? "New high score!" : $"Best  {PrototypeLeaderboard.BestScore}", 15,
                PrototypeLeaderboard.LastRunWasBest ? PrototypeUITheme.Gold : PrototypeUITheme.Muted, TextAnchor.MiddleCenter, true);
        }

        if (PrototypeUITheme.Button(new Rect(rect.x + 50f, rect.y + 240f, width - 100f, 46f), "Retry  (R)")) RestartRun();
        if (PrototypeUITheme.Button(new Rect(rect.x + 50f, rect.y + 298f, width - 100f, 46f), "Main Menu"))
        {
            ResumeGame();
            SceneManager.LoadScene("MainMenu");
        }
    }

    private void RestartRun()
    {
        IsInventoryOpen = false;
        IsPauseMenuOpen = false;
        IsTalentsOpen = false;
        ApplyPause();
        if (run != null) run.Restart();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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

    private void HandleTalentInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) talentCursor = Mathf.Max(0, talentCursor - 1);
        if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) talentCursor = Mathf.Min(3, talentCursor + 1);
        if (keyboard.jKey.wasPressedThisFrame) SpendTalent(talentCursor);
    }

    private void SpendTalent(int index)
    {
        var talents = stats.GetComponent<PrototypeTalents>();
        if (talents == null) return;
        if (talents.TrySpend((PrototypeTalents.Talent)index))
            ShowNotice($"{PrototypeTalents.Names[index]} is now rank {talents.Rank((PrototypeTalents.Talent)index)}.");
    }

    /// <summary>Huntress tier-1 talents: W/S to choose, J to spend a point, T or Esc to close.</summary>
    private void DrawTalents()
    {
        var talents = stats.GetComponent<PrototypeTalents>();
        if (talents == null) { IsTalentsOpen = false; ApplyPause(); return; }
        const float width = 560f;
        const float height = 470f;
        var x = (PrototypeUITheme.Width - width) * 0.5f;
        var y = (PrototypeUITheme.Height - height) * 0.5f;
        PrototypeUITheme.Fill(new Rect(0f, 0f, PrototypeUITheme.Width, PrototypeUITheme.Height), new Color(0f, 0f, 0f, 0.45f));
        PrototypeUITheme.DrawPanel(new Rect(x, y, width, height));
        PrototypeUITheme.DrawIcon(new Rect(x + 24f, y + 20f, 36f, 36f), PrototypeUITheme.Star);
        PrototypeUITheme.Label(new Rect(x + 68f, y + 20f, 300f, 36f), "Huntress Talents", 24, PrototypeUITheme.Gold, TextAnchor.MiddleLeft, true);
        PrototypeUITheme.Label(new Rect(x + width - 220f, y + 20f, 196f, 36f), $"Points: {talents.PointsAvailable}", 18,
            talents.PointsAvailable > 0 ? PrototypeUITheme.Gold : PrototypeUITheme.Muted, TextAnchor.MiddleRight, true);
        for (var i = 0; i < 4; i++)
        {
            var talent = (PrototypeTalents.Talent)i;
            var rank = talents.Rank(talent);
            var row = new Rect(x + 24f, y + 70f + i * 86f, width - 48f, 78f);
            PrototypeUITheme.DrawSlot(row, false, i == talentCursor);
            PrototypeUITheme.Label(new Rect(row.x + 16f, row.y + 8f, 300f, 26f), PrototypeTalents.Names[i], 17, PrototypeUITheme.Gold, TextAnchor.MiddleLeft, true);
            PrototypeUITheme.Label(new Rect(row.xMax - 150f, row.y + 8f, 134f, 26f), $"Rank {rank} / {PrototypeTalents.MaxRank}", 15,
                rank > 0 ? PrototypeUITheme.Parchment : PrototypeUITheme.Muted, TextAnchor.MiddleRight, true);
            PrototypeUITheme.Label(new Rect(row.x + 16f, row.y + 34f, row.width - 32f, 40f),
                PrototypeTalents.Describe(talent, Mathf.Max(1, rank < PrototypeTalents.MaxRank ? rank + 1 : rank)), 13, PrototypeUITheme.Parchment, TextAnchor.UpperLeft, false, true);
            if (GUI.Button(row, GUIContent.none, GUIStyle.none)) { talentCursor = i; SpendTalent(i); }
        }
        PrototypeUITheme.Label(new Rect(x + 24f, y + height - 46f, width - 48f, 28f),
            "W / S: choose    J: spend a point    O / Esc: close    (1 point per level, levels 2-5)", 13, PrototypeUITheme.Muted, TextAnchor.MiddleCenter);
    }

    private static void ResumeGame()
    {
        IsInventoryOpen = false;
        IsTalentsOpen = false;
        IsPauseMenuOpen = false;
        IsTalentsOpen = false;
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
            inventoryMessage ?? "WASD / arrows: move    J: select    Esc / I: close", 13,
            inventoryMessage != null ? PrototypeUITheme.Parchment : PrototypeUITheme.Muted, TextAnchor.MiddleCenter, false, true);
        if (itemActionsOpen) DrawItemActions();
    }

    // Two-press confirmation for risky potion actions (Pixel Dungeon asks before these).
    private int confirmSlot = -1;
    private ItemAction confirmAction;

    private bool ConfirmPending(ItemAction action)
    {
        if (confirmSlot == inventoryCursor && confirmAction == action)
        {
            confirmSlot = -1;
            return true;
        }
        confirmSlot = inventoryCursor;
        confirmAction = action;
        return false;
    }

    private enum ItemAction { Unequip, Consume, Drink, Eat, Read, Equip, EquipRingA, EquipRingB, Drop, Throw }

    private readonly System.Collections.Generic.List<ItemAction> actionBuffer = new();

    /// <summary>Fills actionBuffer with the actions available for the item in a slot.</summary>
    private System.Collections.Generic.List<ItemAction> ActionsFor(PrototypeInventoryItem item, int index)
    {
        actionBuffer.Clear();
        if (item == null) return actionBuffer;
        if (index < 4) actionBuffer.Add(ItemAction.Unequip);
        else if (PrototypePotions.IsPotion(item.ItemId)) actionBuffer.Add(ItemAction.Drink);
        else if (item.ItemId == "food" || item.ItemId == PrototypeItemCatalog.Berry) actionBuffer.Add(ItemAction.Eat);
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
        ItemAction.Drink => "Drink",
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
            case ItemAction.Drink:
            {
                var potions = stats.GetComponent<PrototypePotions>();
                if (potions == null) return;
                // Like Pixel Dungeon, drinking a known harmful potion asks first.
                if (potions.IsKnown(item.ItemId) && PrototypePotions.IsHarmful(item.ItemId) && !ConfirmPending(ItemAction.Drink))
                {
                    inventoryMessage = "Harmful potion! Press J again to drink it anyway.";
                    return;
                }
                var potionId = item.ItemId;
                inventory.ConsumeOneAt(inventoryCursor);
                var message = potions.Drink(potionId);
                stats.GetComponent<PrototypeTalents>()?.OnConsumed();
                PrototypeGameUI.ShowNotice(message);
                inventoryMessage = message;
                itemActionsOpen = false;
                if (potionId == PrototypePotions.LiquidFlame) { IsInventoryOpen = false; ApplyPause(); }
                return;
            }
            case ItemAction.Eat:
                if (item.ItemId == PrototypeItemCatalog.Berry)
                {
                    inventory.ConsumeOneAt(inventoryCursor);
                    hunger.Eat(PrototypeHunger.HungryLevel / 3f);
                    stats.Heal(2);
                }
                else
                {
                    inventory.RemoveAt(inventoryCursor);
                    hunger.Eat(PrototypeHunger.HungryLevel);
                }
                stats.GetComponent<PrototypeTalents>()?.OnConsumed();
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
                // Throwing a known helpful potion asks first (it would just shatter).
                var knownPotions = stats.GetComponent<PrototypePotions>();
                if (knownPotions != null && PrototypePotions.IsPotion(item.ItemId) && knownPotions.IsKnown(item.ItemId)
                    && !PrototypePotions.IsHarmful(item.ItemId) && !ConfirmPending(ItemAction.Throw))
                {
                    inventoryMessage = "Beneficial potion! Press J again to throw it anyway.";
                    return;
                }
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
        else if (PrototypePotions.IsPotion(item.ItemId) && PrototypePotions.Current != null) info = PrototypePotions.Current.Description(item.ItemId);
        else if (PrototypeMeleeWeapons.TryGet(item.ItemId, out var weapon))
            info = $"Damage {weapon.Damage + item.UpgradeLevel}    Reach {weapon.Range:0} tile{(weapon.Range > 1f ? "s" : "")}    Delay {weapon.Cooldown:0.00}s";
        else if (PrototypeArmors.TryGet(item.ItemId, out _))
            info = $"Absorbs 0-{PrototypeArmors.MaxAbsorb(item)} damage per enemy hit";
        var required = PrototypeStrength.Required(item);
        if (required > 0)
        {
            var shortfall = PrototypeStrength.Shortfall(item, stats.Strength);
            info += shortfall > 0
                ? $"\nNeeds {required} Strength (you have {stats.Strength}): " + (item.EquipmentSlot == PrototypeEquipmentSlot.Armor ? "slows you down." : "attacks slower.")
                : $"\nNeeds {required} Strength." + (item.EquipmentSlot == PrototypeEquipmentSlot.Weapon && stats.Strength > required ? " Extra Strength adds bonus damage." : "");
        }
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
        IsTalentsOpen = false;
        Time.timeScale = 1f;
    }
}
