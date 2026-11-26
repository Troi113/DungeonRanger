using UnityEngine;

/// <summary>
/// Persistent user settings (PlayerPrefs): master volume and fullscreen.
/// Applied automatically when the game starts.
/// </summary>
public static class PrototypeSettings
{
    private const string VolumeKey = "settings.volume";
    private const string FullscreenKey = "settings.fullscreen";

    public static float Volume { get; private set; } = 1f;
    public static bool Fullscreen { get; private set; } = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadOnStart()
    {
        Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        Fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        Apply();
    }

    public static void SetVolume(float value)
    {
        Volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumeKey, Volume);
        PlayerPrefs.Save();
        Apply();
    }

    public static void SetFullscreen(bool value)
    {
        Fullscreen = value;
        PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        PlayerPrefs.Save();
        Apply();
    }

    private static void Apply()
    {
        AudioListener.volume = Volume;
        // The editor ignores fullscreen; standalone builds switch modes.
        if (!Application.isEditor) Screen.fullScreen = Fullscreen;
    }
}
