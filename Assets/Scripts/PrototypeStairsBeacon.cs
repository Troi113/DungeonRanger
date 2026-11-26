using UnityEngine;

/// <summary>
/// Presentation only: a soft column of light with rising motes over a staircase. It is unlit and
/// rises well above the walls, so the stairs can be spotted from other rooms and through the fog.
/// Pulses with unscaled time, like PrototypeGlowPulse.
/// </summary>
public sealed class PrototypeStairsBeacon : MonoBehaviour
{
    [SerializeField] private float height = 7f;
    [SerializeField] private float radius = 1.1f;
    [SerializeField] private float pulseSpeed = 2.2f;
    [SerializeField, Range(0f, 1f)] private float pulseDepth = 0.35f;

    private Material beam;
    private Color color;

    public static PrototypeStairsBeacon Add(Transform parent, Vector3 localCenter, Color color, float strength)
    {
        var root = new GameObject("Stairs_Beacon");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localCenter;
        var beacon = root.AddComponent<PrototypeStairsBeacon>();
        beacon.color = new Color(color.r, color.g, color.b, 0.22f * strength);
        beacon.Build(strength);
        return beacon;
    }

    private void Build(float strength)
    {
        // Two nested cylinders: a faint wide halo and a brighter core.
        beam = AdditiveMaterial("Stairs Beam");
        for (var i = 0; i < 2; i++)
        {
            var column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            column.name = i == 0 ? "Beam_Halo" : "Beam_Core";
            column.transform.SetParent(transform, false);
            var r = i == 0 ? radius : radius * 0.45f;
            column.transform.localScale = new Vector3(r * 2f, height * 0.5f, r * 2f);
            column.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            Destroy(column.GetComponent<Collider>());
            var renderer = column.GetComponent<Renderer>();
            renderer.sharedMaterial = beam;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        var motes = new GameObject("Beam_Motes").AddComponent<ParticleSystem>();
        motes.transform.SetParent(transform, false);
        motes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = motes.main;
        main.startLifetime = 3f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startColor = new Color(color.r, color.g, color.b, 0.9f);
        main.maxParticles = 80;
        main.useUnscaledTime = true;
        var emission = motes.emission;
        emission.rateOverTime = 10f * strength;
        var shape = motes.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        var velocity = motes.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        var fade = motes.colorOverLifetime;
        fade.enabled = true;
        fade.color = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) }
        };
        motes.GetComponent<ParticleSystemRenderer>().sharedMaterial = AdditiveMaterial("Stairs Motes");
        motes.Play();
    }

    private void Update()
    {
        var wave = 1f - pulseDepth * 0.5f * (1f + Mathf.Sin(Time.unscaledTime * pulseSpeed));
        if (beam != null) beam.SetColor("_BaseColor", new Color(color.r, color.g, color.b, color.a * wave));
    }

    private void OnDestroy()
    {
        if (beam != null) Destroy(beam);
    }

    private static Material AdditiveMaterial(string name)
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        var material = new Material(shader) { name = name };
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 2f); // additive
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_BLENDMODE_ADD");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_ZWrite", 0);
        material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        material.renderQueue = 3100;
        return material;
    }
}
