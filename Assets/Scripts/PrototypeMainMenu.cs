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

    private void OnGUI()
    {
        PrototypeUITheme.BeginScaledGui();
        PrototypeUITheme.Fill(new Rect(0f, 0f, PrototypeUITheme.Width, PrototypeUITheme.Height), new Color(0.05f, 0.04f, 0.05f));
        const float width = 440f;
        const float height = 400f;
        var x = (PrototypeUITheme.Width - width) * 0.5f;
        var y = (PrototypeUITheme.Height - height) * 0.5f;
        PrototypeUITheme.DrawPanel(new Rect(x, y, width, height));

        PrototypeUITheme.DrawIcon(new Rect(x + 28f, y + 30f, 48f, 48f), PrototypeUITheme.Bow);
        PrototypeUITheme.DrawIcon(new Rect(x + width - 76f, y + 30f, 48f, 48f), PrototypeUITheme.Sword);
        PrototypeUITheme.Label(new Rect(x, y + 26f, width, 52f), "DungeonRanger", 34, PrototypeUITheme.Gold, TextAnchor.MiddleCenter, true);
        PrototypeUITheme.Label(new Rect(x, y + 80f, width, 26f), "Descend. Survive. Claim the Amulet.", 14, PrototypeUITheme.Muted, TextAnchor.MiddleCenter);

        // Main-menu art is 64x32; drawn at whole-number scales to stay crisp.
        if (PrototypeUITheme.ImageButton(new Rect(x + width * 0.5f - 128f, y + 120f, 256f, 128f), PrototypeUITheme.MenuButton(16)))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("DungeonGameplay");
        }
        if (PrototypeUITheme.ImageButton(new Rect(x + width * 0.5f - 96f, y + 262f, 192f, 96f), PrototypeUITheme.MenuButton(13)))
        {
            Application.Quit();
        }
    }
}
