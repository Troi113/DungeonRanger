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
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.045f, 0.065f);
        camera.cullingMask = 0;
        Time.timeScale = 0f;
        SceneManager.UnloadSceneAsync(gameplayScene);
    }

    private void OnGUI()
    {
        var originalColor = GUI.color;
        var originalMatrix = GUI.matrix;
        GUI.color = new Color(0.025f, 0.045f, 0.065f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        var width = Mathf.Min(600f, Screen.width - 32f);
        var height = Mathf.Min(480f, Screen.height - 32f);
        var x = (Screen.width - width) * 0.5f;
        var y = (Screen.height - height) * 0.5f;
        GUI.color = new Color(0.72f, 0.5f, 0.16f);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        GUI.color = new Color(0.06f, 0.09f, 0.12f);
        GUI.DrawTexture(new Rect(x + 4f, y + 4f, width - 8f, height - 8f), Texture2D.whiteTexture);

        var title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 32, fontStyle = FontStyle.Bold };
        title.normal.textColor = new Color(1f, 0.82f, 0.38f);
        var message = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18, wordWrap = true };
        message.normal.textColor = Color.white;
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 16f, y + 24f, width - 32f, 50f), "CONGRATULATIONS!", title);
        GUI.Label(new Rect(x + 16f, y + 78f, width - 32f, 35f), "You found the Amulet of Glory", message);

        var center = new Vector2(Screen.width * 0.5f, y + height * 0.49f);
        GUI.color = new Color(0.9f, 0.63f, 0.14f);
        GUIUtility.RotateAroundPivot(45f, center);
        GUI.DrawTexture(new Rect(center.x - 45f, center.y - 45f, 90f, 90f), Texture2D.whiteTexture);
        GUI.color = new Color(0.2f, 0.8f, 0.9f);
        GUI.DrawTexture(new Rect(center.x - 27f, center.y - 27f, 54f, 54f), Texture2D.whiteTexture);
        GUI.matrix = originalMatrix;
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 16f, y + height * 0.66f, width - 32f, 35f), "YOU WIN!", title);
        if (GUI.Button(new Rect(x + width * 0.5f - 110f, y + height - 80f, 220f, 42f), "Return to Main Menu"))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
        GUI.color = originalColor;
    }
}
