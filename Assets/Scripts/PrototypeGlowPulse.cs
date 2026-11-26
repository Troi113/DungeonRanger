using UnityEngine;

/// <summary>
/// Presentation only: makes an object glow and gently pulse, for goal markers such as stairs.
/// Pulses the material's emission (when given) and an attached point light.
/// </summary>
public sealed class PrototypeGlowPulse : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 2.2f;
    [SerializeField, Range(0f, 1f)] private float pulseDepth = 0.45f;

    private Material material;
    private Light glowLight;
    private Color emission;
    private float baseIntensity;

    /// <summary>Adds an emissive glow to the material and a point light above the object.</summary>
    public static PrototypeGlowPulse Add(GameObject target, Material sharedMaterial, Color color, float emissionStrength, float lightIntensity, float lightRange,
        Vector3? lightOffset = null)
    {
        var pulse = target.AddComponent<PrototypeGlowPulse>();
        pulse.material = sharedMaterial;
        pulse.emission = color * emissionStrength;
        if (sharedMaterial != null && sharedMaterial.HasProperty("_EmissionColor"))
        {
            sharedMaterial.EnableKeyword("_EMISSION");
            sharedMaterial.SetColor("_EmissionColor", pulse.emission);
        }

        var lightObject = new GameObject("Glow_Light");
        lightObject.transform.SetParent(target.transform, false);
        lightObject.transform.localPosition = lightOffset ?? new Vector3(0f, 1.2f, 0f);
        pulse.glowLight = lightObject.AddComponent<Light>();
        pulse.glowLight.type = LightType.Point;
        pulse.glowLight.color = color;
        pulse.glowLight.range = lightRange;
        pulse.glowLight.intensity = lightIntensity;
        pulse.glowLight.shadows = LightShadows.None;
        pulse.baseIntensity = lightIntensity;
        return pulse;
    }

    private void Update()
    {
        // Unscaled time keeps the beacon pulsing even while the game is paused.
        var wave = 1f - pulseDepth * 0.5f * (1f + Mathf.Sin(Time.unscaledTime * pulseSpeed));
        if (glowLight != null) glowLight.intensity = baseIntensity * wave;
        if (material != null && material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission * wave);
    }
}
