using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Background music that follows the game: menu theme on the main menu, dungeon ambience on
/// floors 1-4, the Goo battle theme on floor 5, and the victory fanfare on the ending screen.
/// Tracks load from Resources/Music (Assets/Audio/Resources/Music); replace a WAV/OGG/MP3
/// with the same name to swap a song. Crossfades between tracks; overall volume follows
/// Settings (AudioListener.volume). Created automatically and kept across scenes.
/// </summary>
public sealed class PrototypeMusic : MonoBehaviour
{
    public const string MenuTrack = "menu_theme";
    public const string DungeonTrack = "dungeon";
    public const string BossTrack = "boss_goo";
    public const string VictoryTrack = "victory";

    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.55f;
    [SerializeField] private float fadeSeconds = 1.5f;
    [SerializeField] private float checkInterval = 0.5f;

    private static PrototypeMusic instance;
    private AudioSource current;
    private AudioSource previous;
    private string currentTrack;
    private float fade = 1f;
    private float nextCheck;
    private PrototypeDungeonRun run;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (instance != null) return;
        var host = new GameObject("Music");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<PrototypeMusic>();
    }

    private void Awake()
    {
        current = NewSource();
        previous = NewSource();
        SceneManager.activeSceneChanged += (_, _) => { run = null; nextCheck = 0f; };
    }

    private AudioSource NewSource()
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
        return source;
    }

    private void Update()
    {
        // Unscaled time: music keeps fading and switching while the game is paused.
        if (Time.unscaledTime >= nextCheck)
        {
            nextCheck = Time.unscaledTime + checkInterval;
            var wanted = WantedTrack();
            if (wanted != currentTrack) Play(wanted);
        }

        if (fade < 1f) fade = Mathf.Min(1f, fade + Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds));
        current.volume = musicVolume * fade;
        previous.volume = musicVolume * (1f - fade);
        if (fade >= 1f && previous.isPlaying) previous.Stop();
    }

    private string WantedTrack()
    {
        var scene = SceneManager.GetActiveScene().name;
        if (scene == "MainMenu") return MenuTrack;
        if (scene == "Victory") return VictoryTrack;
        if (run == null) run = FindAnyObjectByType<PrototypeDungeonRun>();
        if (run == null) return currentTrack;
        return run.Floor >= 5 && !run.Victory ? BossTrack : DungeonTrack;
    }

    private void Play(string trackName)
    {
        currentTrack = trackName;
        if (string.IsNullOrEmpty(trackName)) return;
        var clip = Resources.Load<AudioClip>("Music/" + trackName);
        if (clip == null)
        {
            Debug.LogWarning($"Music track missing: Resources/Music/{trackName}");
            return;
        }

        (previous, current) = (current, previous);
        current.clip = clip;
        current.loop = trackName != VictoryTrack;
        current.volume = 0f;
        current.Play();
        fade = 0f;
    }
}
