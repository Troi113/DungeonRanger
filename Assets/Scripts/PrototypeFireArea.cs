using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A short-lived patch of fire (Potion of Liquid Flame). Anything standing in it burns:
/// the ranger catches fire (Burning: 1 damage per second, lasting a moment after leaving),
/// and enemies take 1 damage every second they stay inside.
/// </summary>
public sealed class PrototypeFireArea : MonoBehaviour
{
    [SerializeField] private float radius = 5f;
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private float tickSeconds = 1f;
    [SerializeField] private float burnSeconds = 3f;

    private float age;
    private float nextTick;
    private Light glow;
    private readonly List<PrototypeEnemy> buffer = new();

    public static PrototypeFireArea Ignite(Transform parent, Vector3 position)
    {
        var obj = new GameObject("Liquid_Flame");
        obj.transform.SetParent(parent, true);
        obj.transform.position = new Vector3(position.x, 0.05f, position.z);
        var fire = obj.AddComponent<PrototypeFireArea>();
        fire.BuildVisuals();
        PrototypeGameFeel.Shake(0.25f, 0.2f);
        return fire;
    }

    private void BuildVisuals()
    {
        var particles = gameObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.duration = lifetime;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.35f, 0.05f));
        main.gravityModifier = -0.2f;
        main.stopAction = ParticleSystemStopAction.None;
        var emission = particles.emission;
        emission.rateOverTime = 90f;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        GetComponent<ParticleSystemRenderer>().sharedMaterial = new Material(shader) { name = "Flame" };
        particles.Play();

        glow = new GameObject("Flame_Light").AddComponent<Light>();
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = Vector3.up * 1.5f;
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.55f, 0.2f);
        glow.range = radius * 2.5f;
        glow.intensity = 4f;
        glow.shadows = LightShadows.None;
    }

    private void Update()
    {
        if (PrototypeGameUI.IsGamePaused) return;
        age += Time.deltaTime;
        if (glow != null) glow.intensity = 4f * Mathf.Clamp01((lifetime - age) / 1.5f) * (0.85f + 0.15f * Mathf.PerlinNoise(age * 8f, 0f));
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        var player = FindAnyObjectByType<PrototypePlayerMovement>();
        if (player != null && Inside(player.transform.position))
            player.GetComponent<PrototypeStatusEffects>()?.Apply(PrototypeStatusEffectType.Burning, burnSeconds);

        if (age < nextTick) return;
        nextTick = age + tickSeconds;
        buffer.Clear();
        buffer.AddRange(FindObjectsByType<PrototypeEnemy>(FindObjectsInactive.Exclude));
        foreach (var enemy in buffer)
            if (enemy != null && enemy.IsAlive && Inside(enemy.transform.position)) enemy.TakeDamage(1);
    }

    private bool Inside(Vector3 position)
    {
        var offset = position - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= radius * radius;
    }
}
