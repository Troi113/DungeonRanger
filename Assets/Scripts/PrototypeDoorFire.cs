using System.Collections;
using UnityEngine;

/// <summary>
/// Presentation only: a burning door. Flames and smoke rise off the planks and an orange
/// light flickers while the wood chars from brown to glowing black; when the door burns
/// through, the planks topple and fall, shrink into embers, and a few embers keep drifting up.
/// </summary>
public sealed class PrototypeDoorFire : MonoBehaviour
{
    [SerializeField] private float lightIntensity = 3f;
    [SerializeField] private float lightRange = 7f;

    private Transform model;
    private Renderer[] renderers;
    private Color[] startColors;
    private ParticleSystem flames;
    private ParticleSystem smoke;
    private Light glow;
    private static Material additive;
    private static Material alpha;

    public static PrototypeDoorFire Start(Transform model, Vector3 size)
    {
        var fire = model.gameObject.AddComponent<PrototypeDoorFire>();
        fire.Begin(model, size);
        return fire;
    }

    private void Begin(Transform doorModel, Vector3 size)
    {
        model = doorModel;
        renderers = model.GetComponentsInChildren<Renderer>();
        startColors = new Color[renderers.Length];
        for (var i = 0; i < renderers.Length; i++)
        {
            // Own copies so only this door chars.
            renderers[i].material = new Material(renderers[i].sharedMaterial);
            startColors[i] = renderers[i].material.color;
        }
        flames = Emitter("Door_Flames", size, new Color(1f, 0.55f, 0.15f, 0.9f), 0.35f, 0.9f, 0.5f, 0.9f, 2.5f, 45f, true);
        smoke = Emitter("Door_Smoke", size, new Color(0.12f, 0.1f, 0.09f, 0.5f), 0.8f, 1.6f, 1.4f, 2.2f, 1.6f, 10f, false);
        glow = new GameObject("Door_Fire_Light").AddComponent<Light>();
        glow.transform.SetParent(model, false);
        glow.transform.localPosition = Vector3.up * size.y * 0.2f;
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.5f, 0.18f);
        glow.range = lightRange;
        glow.shadows = LightShadows.None;
    }

    /// <summary>0 = fresh wood, 1 = burnt through. Called each frame by the door while it burns.</summary>
    public void SetProgress(float progress)
    {
        var flicker = 0.75f + 0.25f * Mathf.PerlinNoise(Time.time * 6f, 0.3f);
        if (glow != null) glow.intensity = lightIntensity * flicker * Mathf.Clamp01(progress * 4f);
        var char_ = new Color(0.07f, 0.04f, 0.03f);
        var ember = new Color(0.9f, 0.25f, 0.04f) * (0.6f * progress * flicker);
        for (var i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            var material = renderers[i].material;
            material.color = Color.Lerp(startColors[i], char_, Mathf.SmoothStep(0f, 1f, progress));
            if (!material.HasProperty("_EmissionColor")) continue;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", ember);
        }
    }

    /// <summary>The door burns through: the pieces fall and smoulder away, then everything is removed.</summary>
    public void Collapse() => StartCoroutine(CollapseRoutine());

    private IEnumerator CollapseRoutine()
    {
        // Only the wooden and iron pieces fall; the flames, smoke and light stay put.
        var list = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in model)
            if (child.name.StartsWith("Plank_") || child.name.StartsWith("Iron_")) list.Add(child);
        var pieces = list.ToArray();
        var spin = new Vector3[pieces.Length];
        var drift = new Vector3[pieces.Length];
        var startScale = new Vector3[pieces.Length];
        for (var i = 0; i < pieces.Length; i++)
        {
            startScale[i] = pieces[i].localScale;
            spin[i] = new Vector3(Random.Range(-160f, 160f), Random.Range(-40f, 40f), Random.Range(-90f, 90f));
            drift[i] = new Vector3(Random.Range(-0.6f, 0.6f), 0f, Random.Range(-1.2f, 1.2f));
        }

        // Topple and fall to the floor.
        const float fall = 0.7f;
        for (var t = 0f; t < fall; t += Time.deltaTime)
        {
            var step = Time.deltaTime;
            for (var i = 0; i < pieces.Length; i++)
            {
                var piece = pieces[i];
                if (piece == null) continue;
                piece.Rotate(spin[i] * step, Space.Self);
                var drop = 9f * t * step;
                piece.position += model.TransformVector(drift[i]) * step + Vector3.down * drop;
                if (piece.position.y < 0.1f) piece.position = new Vector3(piece.position.x, 0.1f, piece.position.z);
            }
            yield return null;
        }

        if (flames != null) { var emission = flames.emission; emission.rateOverTime = 8f; }
        // Smoulder into embers.
        const float shrink = 1.6f;
        for (var t = 0f; t < shrink; t += Time.deltaTime)
        {
            var k = 1f - t / shrink;
            for (var i = 0; i < pieces.Length; i++)
                if (pieces[i] != null) pieces[i].localScale = Vector3.Scale(startScale[i], new Vector3(k, k * k, k));
            if (glow != null) glow.intensity = lightIntensity * 0.5f * k;
            yield return null;
        }

        if (flames != null) flames.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (smoke != null) smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        for (var i = 0; i < pieces.Length; i++)
            if (pieces[i] != null) pieces[i].gameObject.SetActive(false);
        if (glow != null) Destroy(glow.gameObject);
        yield return new WaitForSeconds(3f);
        Destroy(model.gameObject);
    }

    private ParticleSystem Emitter(string name, Vector3 size, Color color, float minSize, float maxSize, float minLife, float maxLife,
        float rise, float rate, bool additiveBlend)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(model, false);
        var particles = obj.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startSpeed = 0f;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;
        var emission = particles.emission;
        emission.rateOverTime = rate;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(size.x, size.y * 0.9f, size.z + 0.1f);
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(rise * 0.6f, rise);
        var sizeOverLife = particles.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, additiveBlend
            ? new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.2f))
            : new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.6f)));
        var fade = particles.colorOverLifetime;
        fade.enabled = true;
        fade.color = new Gradient
        {
            colorKeys = additiveBlend
                ? new[] { new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.05f), 1f) }
                : new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) }
        };
        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = additiveBlend ? Additive() : Alpha();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particles.Play();
        return particles;
    }

    private static Material Additive() => additive != null ? additive : additive = ParticleMaterial("Door Fire", true);
    private static Material Alpha() => alpha != null ? alpha : alpha = ParticleMaterial("Door Smoke", false);

    private static Material ParticleMaterial(string name, bool add)
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        var material = new Material(shader) { name = name };
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", add ? 2f : 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        if (add) material.EnableKeyword("_BLENDMODE_ADD");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)(add ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
        material.SetInt("_ZWrite", 0);
        material.renderQueue = 3000;
        return material;
    }
}
