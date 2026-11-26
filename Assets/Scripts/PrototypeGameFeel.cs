using System.Collections;
using UnityEngine;

/// <summary>
/// Presentation-only impact effects: hit-stop (a brief near-freeze of game time), camera shake,
/// and spark bursts. Created on demand and kept across scenes.
/// </summary>
public sealed class PrototypeGameFeel : MonoBehaviour
{
    private static PrototypeGameFeel instance;
    private Coroutine hitStop;
    private Material sparkMaterial;

    private static PrototypeGameFeel Instance
    {
        get
        {
            if (instance != null) return instance;
            var host = new GameObject("GameFeel");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<PrototypeGameFeel>();
            return instance;
        }
    }

    /// <summary>Slows time almost to a stop for a moment so a hit lands with weight.</summary>
    public static void HitStop(float seconds)
    {
        if (PrototypeGameUI.IsGamePaused || seconds <= 0f) return;
        var feel = Instance;
        if (feel.hitStop != null) feel.StopCoroutine(feel.hitStop);
        feel.hitStop = feel.StartCoroutine(feel.HitStopRoutine(seconds));
    }

    public static void Shake(float strength, float seconds) => PrototypeCameraFollow.AddShake(strength, seconds);

    /// <summary>A quick burst of glowing sparks at a point.</summary>
    public static void Sparks(Vector3 position, Color color, int count = 14)
    {
        var feel = Instance;
        var obj = new GameObject("Hit_Sparks");
        obj.transform.position = position;
        var particles = obj.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 13f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startColor = color;
        main.gravityModifier = 1.5f;
        main.useUnscaledTime = true;
        main.stopAction = ParticleSystemStopAction.Destroy;
        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, (short)count));
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;
        var sizeOverLife = particles.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        if (feel.sparkMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            feel.sparkMaterial = new Material(shader) { name = "Hit Sparks" };
        }
        renderer.sharedMaterial = feel.sparkMaterial;
        particles.Play();
    }

    private IEnumerator HitStopRoutine(float seconds)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(seconds);
        Time.timeScale = PrototypeGameUI.IsGamePaused ? 0f : 1f;
        hitStop = null;
    }
}
