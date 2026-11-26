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

    private enum MenuView { Main, GamesInProgress, ConfirmNewGame, Leaderboards }

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
        const float height = 470f;
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
        if (PrototypeUITheme.ImageButton(new Rect(x + width * 0.5f - 96f, y + 320f, 192f, 96f), PrototypeUITheme.MenuButton(13)))
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
        var panel = Panel(460f, 300f, "Leaderboards", PrototypeUITheme.Trophy);
        PrototypeUITheme.Label(new Rect(panel.x + 36f, panel.y + 90f, panel.width - 72f, 40f), "No entries yet.", 15, PrototypeUITheme.Muted, TextAnchor.MiddleCenter);
        if (PrototypeUITheme.Button(new Rect(panel.x + 40f, panel.y + 210f, panel.width - 80f, 48f), "Back")) view = MenuView.Main;
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
