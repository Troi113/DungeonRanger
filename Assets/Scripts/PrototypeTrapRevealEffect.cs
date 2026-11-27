using UnityEngine;

/// <summary>
/// Presentation only: the "dusted off" burst when a hidden trap is discovered - a puff of dust
/// rolling outward over the floor plus bright sparkles rising off the trap. The object removes
/// itself when the particles finish.
/// </summary>
public static class PrototypeTrapRevealEffect
{
    private static Material material;

    public static void Play(Vector3 position, float radius)
    {
        var root = new GameObject("Trap_Reveal_Effect");
        root.transform.position = position + Vector3.up * 0.15f;
        Burst(root.transform, "Dust", new Color(0.62f, 0.56f, 0.46f, 0.55f), 36, 0.7f, 1.4f, 1.2f, 2.2f, 0.8f, 1.6f, radius, 0.3f, 3f);
        Burst(root.transform, "Sparkles", new Color(1f, 0.95f, 0.7f, 1f), 26, 0.08f, 0.2f, 0.5f, 1.1f, 1.5f, 3.5f, radius * 0.7f, -0.6f, 0.5f);
        Object.Destroy(root, 2.5f);
    }

    private static void Burst(Transform parent, string name, Color color, int count, float minSize, float maxSize,
        float minLife, float maxLife, float minSpeed, float maxSpeed, float radius, float gravity, float drag)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var particles = obj.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startColor = color;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count;
        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = radius;
        var limit = particles.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.drag = drag;
        limit.limit = 100f;
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1f, 1.3f)));
        var fade = particles.colorOverLifetime;
        fade.enabled = true;
        fade.color = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) }
        };
        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = Material();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particles.Play();
    }

    private static Material Material()
    {
        if (material != null) return material;
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        material = new Material(shader) { name = "Trap Reveal Particles" };
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.renderQueue = 3000;
        return material;
    }
}
