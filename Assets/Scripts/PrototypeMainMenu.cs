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
        const float width = 360f;
        const float height = 260f;
        var x = (Screen.width - width) * 0.5f;
        var y = (Screen.height - height) * 0.5f;
        GUI.Box(new Rect(x, y, width, height), "DungeonRanger");
        if (GUI.Button(new Rect(x + 55f, y + 75f, width - 110f, 50f), "Start Game"))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("DungeonGameplay");
        }
        if (GUI.Button(new Rect(x + 55f, y + 145f, width - 110f, 50f), "Exit Game"))
        {
            Application.Quit();
        }
    }
}
