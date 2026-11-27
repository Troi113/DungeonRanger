using UnityEngine;

/// <summary>
/// Presentation only: the dark dungeon mood. Dims the sun-like directional light and the
/// ambient light so rooms read as dark stone, and hangs a warm lantern light on the ranger so
/// the area around them (inside the fog of war's view) stays readable. Torches, stairs beacons,
/// drop glows and fire stand out against the dark.
/// </summary>
public sealed class PrototypeDungeonLighting : MonoBehaviour
{
    [SerializeField] private float directionalIntensity = 0.35f;
    [SerializeField] private Color directionalColor = new(0.7f, 0.76f, 0.95f);
    [SerializeField] private Color ambientSky = new(0.07f, 0.075f, 0.09f);
    [SerializeField] private Color ambientEquator = new(0.045f, 0.05f, 0.055f);
    [SerializeField] private Color ambientGround = new(0.02f, 0.02f, 0.02f);
    [SerializeField] private Color lanternColor = new(1f, 0.84f, 0.62f);
    [SerializeField] private float lanternIntensity = 1.8f;
    [SerializeField] private float lanternRange = 18f;
    [SerializeField] private float lanternHeight = 3f;

    public void Apply(Transform player)
    {
        Light sun = null;
        foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (light.type != LightType.Directional) continue;
            sun = light;
            break;
        }
        if (sun != null)
        {
            sun.intensity = directionalIntensity;
            sun.color = directionalColor;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;

        if (player == null) return;
        var lantern = new GameObject("Hero_Lantern").AddComponent<Light>();
        lantern.transform.SetParent(player, false);
        lantern.transform.localPosition = Vector3.up * lanternHeight;
        lantern.type = LightType.Point;
        lantern.color = lanternColor;
        lantern.intensity = lanternIntensity;
        lantern.range = lanternRange;
        lantern.shadows = LightShadows.None;
    }
}
