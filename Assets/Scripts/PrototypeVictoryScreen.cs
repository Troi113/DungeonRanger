using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Creates a dedicated runtime ending scene after the amulet is collected.</summary>
public sealed class PrototypeVictoryScreen : MonoBehaviour
{
    public static void Show()
    {
        var gameplayScene = SceneManager.GetActiveScene();
        var endingScene = SceneManager.CreateScene("Victory");
        SceneManager.SetActiveScene(endingScene);
        var screen = new GameObject("VictoryScreen");
        SceneManager.MoveGameObjectToScene(screen, endingScene);
        screen.AddComponent<PrototypeVictoryScreen>();
        var camera = screen.AddComponent<Camera>();
        // The gameplay scene (and its listener) unloads, so the ending needs its own to hear the fanfare.
        screen.AddComponent<AudioListener>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.045f, 0.065f);
        camera.cullingMask = 0;
        Time.timeScale = 0f;
        SceneManager.UnloadSceneAsync(gameplayScene);
    }

    private void OnGUI()
    {
        PrototypeUITheme.BeginScaledGui();
        PrototypeUITheme.Fill(new Rect(0f, 0f, PrototypeUITheme.Width, PrototypeUITheme.Height), new Color(0.025f, 0.045f, 0.065f));

        var width = Mathf.Min(600f, PrototypeUITheme.Width - 32f);
        var height = Mathf.Min(480f, PrototypeUITheme.Height - 32f);
        var x = (PrototypeUITheme.Width - width) * 0.5f;
        var y = (PrototypeUITheme.Height - height) * 0.5f;
        PrototypeUITheme.DrawPanel(new Rect(x, y, width, height));

        PrototypeUITheme.Label(new Rect(x + 16f, y + 30f, width - 32f, 50f), "CONGRATULATIONS!", 34, PrototypeUITheme.Gold, TextAnchor.MiddleCenter, true);
        PrototypeUITheme.Label(new Rect(x + 16f, y + 84f, width - 32f, 32f), "You found the Amulet of Glory", 18, PrototypeUITheme.Parchment, TextAnchor.MiddleCenter);

        var center = new Vector2(PrototypeUITheme.Width * 0.5f, y + height * 0.47f);
        PrototypeUITheme.DrawIcon(new Rect(center.x - 48f, center.y - 66f, 96f, 96f), PrototypeUITheme.Trophy);
        PrototypeUITheme.DrawIcon(new Rect(center.x - 116f, center.y - 34f, 48f, 48f), PrototypeUITheme.Crown);
        PrototypeUITheme.DrawIcon(new Rect(center.x + 68f, center.y - 34f, 48f, 48f), PrototypeUITheme.Ring);
        PrototypeUITheme.Label(new Rect(x + 16f, y + height * 0.6f, width - 32f, 50f), "YOU WIN!", 34, PrototypeUITheme.Gold, TextAnchor.MiddleCenter, true);
        var result = PrototypeLeaderboard.LastRun;
        if (result != null)
            PrototypeUITheme.Label(new Rect(x + 16f, y + height * 0.6f + 48f, width - 32f, 28f),
                $"Score {result.Score}    Time {PrototypeLeaderboard.FormatTime(result.Seconds)}" + (PrototypeLeaderboard.LastRunWasBest ? "    New high score!" : ""),
                17, PrototypeUITheme.Parchment, TextAnchor.MiddleCenter, true);
        if (PrototypeUITheme.Button(new Rect(x + width * 0.5f - 130f, y + height - 88f, 260f, 50f), "Return to Main Menu"))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
