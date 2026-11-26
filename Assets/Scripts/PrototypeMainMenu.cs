using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Simple title screen for the playable prototype.</summary>
public sealed class PrototypeMainMenu : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHandler()
    {
        SceneManager.sceneLoaded -= CreateMenu;
        SceneManager.sceneLoaded += CreateMenu;
    }

    private static void CreateMenu(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainMenu") return;
        Time.timeScale = 1f;
        new GameObject("MainMenuUI").AddComponent<PrototypeMainMenu>();
    }

    private enum MenuView { Main, GamesInProgress, ConfirmNewGame, Leaderboards, Settings }

    private MenuView view = MenuView.Main;
    private PrototypeSaveData savedGame;

    private void OnGUI()
    {
        PrototypeUITheme.BeginScaledGui();
        PrototypeUITheme.Fill(new Rect(0f, 0f, PrototypeUITheme.Width, PrototypeUITheme.Height), new Color(0.05f, 0.04f, 0.05f));
        switch (view)
        {
            case MenuView.Main: DrawMain(); break;
            case MenuView.GamesInProgress: DrawGamesInProgress(); break;
            case MenuView.ConfirmNewGame: DrawConfirmNewGame(); break;
            case MenuView.Leaderboards: DrawLeaderboards(); break;
            case MenuView.Settings: DrawSettings(); break;
        }
    }

    private static Rect Panel(float width, float height, string title, Texture2D icon = null)
    {
        var rect = new Rect((PrototypeUITheme.Width - width) * 0.5f, (PrototypeUITheme.Height - height) * 0.5f, width, height);
        PrototypeUITheme.DrawPanel(rect);
        if (icon != null) PrototypeUITheme.DrawIcon(new Rect(rect.x + 26f, rect.y + 24f, 36f, 36f), icon);
        PrototypeUITheme.Label(new Rect(rect.x, rect.y + 22f, rect.width, 40f), title, 24, PrototypeUITheme.Gold, TextAnchor.MiddleCenter, true);
        return rect;
    }

    private void DrawMain()
    {
        const float width = 440f;
        const float height = 530f;
        var x = (PrototypeUITheme.Width - width) * 0.5f;
        var y = (PrototypeUITheme.Height - height) * 0.5f;
        PrototypeUITheme.DrawPanel(new Rect(x, y, width, height));

        PrototypeUITheme.DrawIcon(new Rect(x + 28f, y + 30f, 48f, 48f), PrototypeUITheme.Bow);
        PrototypeUITheme.DrawIcon(new Rect(x + width - 76f, y + 30f, 48f, 48f), PrototypeUITheme.Sword);
        PrototypeUITheme.Label(new Rect(x, y + 26f, width, 52f), "DungeonRanger", 34, PrototypeUITheme.Gold, TextAnchor.MiddleCenter, true);
        PrototypeUITheme.Label(new Rect(x, y + 80f, width, 26f), "Descend. Survive. Claim the Amulet.", 14, PrototypeUITheme.Muted, TextAnchor.MiddleCenter);

        // Main-menu art is 64x32; drawn at whole-number scales to stay crisp.
        if (PrototypeUITheme.ImageButton(new Rect(x + width * 0.5f - 128f, y + 116f, 256f, 128f), PrototypeUITheme.MenuButton(16)))
        {
            savedGame = PrototypeSaveSystem.Load();
            view = MenuView.GamesInProgress;
        }
        if (PrototypeUITheme.Button(new Rect(x + 70f, y + 256f, width - 140f, 48f), "Leaderboards")) view = MenuView.Leaderboards;
        if (PrototypeUITheme.Button(new Rect(x + 70f, y + 314f, width - 140f, 48f), "Settings")) view = MenuView.Settings;
        if (PrototypeUITheme.ImageButton(new Rect(x + width * 0.5f - 96f, y + 380f, 192f, 96f), PrototypeUITheme.MenuButton(13)))
            Application.Quit();
    }

    private void DrawGamesInProgress()
    {
        var hasSave = savedGame != null;
        var panel = Panel(460f, hasSave ? 330f : 280f, "Games in Progress", PrototypeUITheme.Castle);
        var x = panel.x + 40f;
        var width = panel.width - 80f;
        var y = panel.y + 80f;

        if (hasSave)
        {
            var played = savedGame.LastPlayedLocal;
            var playedText = played == System.DateTime.MinValue ? "unknown" : played.ToString("MMM d, yyyy  h:mm tt");
            var floorText = savedGame.Floor >= 5 ? "Floor 5 (Goo)" : $"Floor {savedGame.Floor}";
            var slot = new Rect(x, y, width, 76f);
            var hovered = slot.Contains(Event.current.mousePosition);
            PrototypeUITheme.DrawSlot(slot, false, hovered);
            PrototypeUITheme.DrawIcon(new Rect(slot.x + 16f, slot.y + 16f, 44f, 44f), PrototypeUITheme.Bow);
            PrototypeUITheme.Label(new Rect(slot.x + 72f, slot.y + 10f, slot.width - 84f, 30f), $"Continue  -  {floorText}", 17, PrototypeUITheme.Gold, TextAnchor.MiddleLeft, true);
            PrototypeUITheme.Label(new Rect(slot.x + 72f, slot.y + 40f, slot.width - 84f, 26f), $"Last played: {playedText}", 13, PrototypeUITheme.Parchment);
            if (GUI.Button(slot, GUIContent.none, GUIStyle.none))
            {
                PrototypeSaveSystem.RequestContinue(savedGame);
                StartGameplay();
            }
            y += 96f;
        }
        else
        {
            PrototypeUITheme.Label(new Rect(x, y, width, 30f), "No game in progress.", 15, PrototypeUITheme.Muted, TextAnchor.MiddleCenter);
            y += 50f;
        }

        if (PrototypeUITheme.Button(new Rect(x, y, width, 48f), "New Game"))
        {
            if (hasSave) view = MenuView.ConfirmNewGame;
            else StartNewGame();
        }
        if (PrototypeUITheme.Button(new Rect(x, y + 64f, width, 48f), "Back")) view = MenuView.Main;
    }

    private void DrawConfirmNewGame()
    {
        var panel = Panel(460f, 260f, "New Game", PrototypeUITheme.Exclamation);
        PrototypeUITheme.Label(new Rect(panel.x + 36f, panel.y + 76f, panel.width - 72f, 56f),
            "Starting a new game will erase your game in progress.", 15, PrototypeUITheme.Parchment, TextAnchor.MiddleCenter, false, true);
        if (PrototypeUITheme.Button(new Rect(panel.x + 36f, panel.y + 160f, 180f, 48f), "Start New Game")) StartNewGame();
        if (PrototypeUITheme.Button(new Rect(panel.xMax - 216f, panel.y + 160f, 180f, 48f), "Cancel")) view = MenuView.GamesInProgress;
    }

    private void DrawLeaderboards()
    {
        var entries = PrototypeLeaderboard.Entries;
        var rows = Mathf.Max(1, entries.Count);
        var panel = Panel(520f, 190f + rows * 30f, "Leaderboards", PrototypeUITheme.Trophy);
        var x = panel.x + 36f;
        var y = panel.y + 74f;
        if (entries.Count == 0)
            PrototypeUITheme.Label(new Rect(x, y, panel.width - 72f, 30f), "No runs recorded yet.", 15, PrototypeUITheme.Muted, TextAnchor.MiddleCenter);
        else
        {
            PrototypeUITheme.Label(new Rect(x, y, 40f, 24f), "#", 13, PrototypeUITheme.Muted);
            PrototypeUITheme.Label(new Rect(x + 40f, y, 90f, 24f), "Score", 13, PrototypeUITheme.Muted);
            PrototypeUITheme.Label(new Rect(x + 130f, y, 110f, 24f), "Result", 13, PrototypeUITheme.Muted);
            PrototypeUITheme.Label(new Rect(x + 240f, y, 70f, 24f), "Time", 13, PrototypeUITheme.Muted);
            PrototypeUITheme.Label(new Rect(x + 310f, y, 140f, 24f), "Date", 13, PrototypeUITheme.Muted);
            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                var rowY = y + 26f + i * 30f;
                var color = i == 0 ? PrototypeUITheme.Gold : PrototypeUITheme.Parchment;
                PrototypeUITheme.Label(new Rect(x, rowY, 40f, 28f), (i + 1).ToString(), 15, color, TextAnchor.MiddleLeft, true);
                PrototypeUITheme.Label(new Rect(x + 40f, rowY, 90f, 28f), e.Score.ToString(), 15, color, TextAnchor.MiddleLeft, true);
                PrototypeUITheme.Label(new Rect(x + 130f, rowY, 110f, 28f), e.Victory ? "Victory" : $"Floor {e.Floor}", 14, e.Victory ? PrototypeUITheme.Gold : PrototypeUITheme.Parchment);
                PrototypeUITheme.Label(new Rect(x + 240f, rowY, 70f, 28f), PrototypeLeaderboard.FormatTime(e.Seconds), 14, PrototypeUITheme.Parchment);
                PrototypeUITheme.Label(new Rect(x + 310f, rowY, 140f, 28f), e.DateText, 13, PrototypeUITheme.Muted);
            }
        }
        if (PrototypeUITheme.Button(new Rect(panel.x + 40f, panel.yMax - 72f, panel.width - 80f, 48f), "Back")) view = MenuView.Main;
    }

    private void DrawSettings()
    {
        var panel = Panel(460f, 330f, "Settings", PrototypeUITheme.Icon(1, 3));
        var x = panel.x + 40f;
        var width = panel.width - 80f;
        PrototypeUITheme.Label(new Rect(x, panel.y + 80f, width, 26f), $"Volume  {Mathf.RoundToInt(PrototypeSettings.Volume * 100f)}%", 16, PrototypeUITheme.Parchment, TextAnchor.MiddleLeft, true);
        var volume = GUI.HorizontalSlider(new Rect(x, panel.y + 114f, width, 20f), PrototypeSettings.Volume, 0f, 1f);
        if (!Mathf.Approximately(volume, PrototypeSettings.Volume)) PrototypeSettings.SetVolume(volume);
        if (PrototypeUITheme.Button(new Rect(x, panel.y + 152f, width, 46f), PrototypeSettings.Fullscreen ? "Fullscreen: On" : "Fullscreen: Off"))
            PrototypeSettings.SetFullscreen(!PrototypeSettings.Fullscreen);
        if (PrototypeUITheme.Button(new Rect(x, panel.y + 250f, width, 46f), "Back")) view = MenuView.Main;
    }

    private static void StartNewGame()
    {
        PrototypeSaveSystem.Delete();
        PrototypeSaveSystem.RequestContinue(null);
        StartGameplay();
    }

    private static void StartGameplay()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("DungeonGameplay");
    }
}
