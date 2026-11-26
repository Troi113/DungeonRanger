using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Presentation only: Goo's death. The slime swells and wobbles for a moment, then bursts into
/// dozens of glossy goo blobs (plus its two eyes) that arc out, bounce and squish on the floor,
/// settle into little puddles, and finally shrink away. Created at Goo's position when it dies,
/// so it outlives the destroyed boss object.
/// </summary>
public sealed class PrototypeGooExplosion : MonoBehaviour
{
    private const float SwellSeconds = 0.35f;
    private const float SettleSeconds = 3.5f;
    private const float FadeSeconds = 1.5f;
    private const float Gravity = -28f;

    private sealed class Blob
    {
        public Transform Transform;
        public Vector3 Velocity;
        public float Radius;
        public bool Resting;
        public float Squish;
    }

    private readonly List<Blob> blobs = new();
    private Transform swell;
    private Vector3 bodySize;
    private Color color;
    private Material gooMaterial;
    private Material eyeWhite;
    private Material eyeBlack;
    private float age;
    private bool burst;

    public static void Spawn(Transform parent, Vector3 position, Vector3 bodySize, Color color)
    {
        var obj = new GameObject("Goo_Explosion");
        obj.transform.SetParent(parent, true);
        obj.transform.position = new Vector3(position.x, 0f, position.z);
        var explosion = obj.AddComponent<PrototypeGooExplosion>();
        explosion.bodySize = bodySize;
        explosion.color = color;
        explosion.Begin();
    }

    private void Begin()
    {
        gooMaterial = PrototypeBootstrap.CreateMaterial("Goo Bits", color);
        if (gooMaterial.HasProperty("_Smoothness")) gooMaterial.SetFloat("_Smoothness", 0.85f);
        eyeWhite = PrototypeBootstrap.CreateMaterial("Goo Bits Eye", Color.white);
        eyeBlack = PrototypeBootstrap.CreateMaterial("Goo Bits Pupil", new Color(0.08f, 0.04f, 0.04f));

        // A stand-in body that swells before the burst (the real Goo object is already gone).
        swell = Sphere("Goo_Swell", transform, gooMaterial).transform;
        swell.localPosition = new Vector3(0f, bodySize.y * 0.5f, 0f);
        swell.localScale = bodySize;
        PrototypeGameFeel.HitStop(0.12f);
        PrototypeGameFeel.Shake(0.4f, 0.3f);
    }

    private void Update()
    {
        if (PrototypeGameUI.IsGamePaused) return;
        var dt = Time.deltaTime;
        age += dt;

        if (!burst)
        {
            // Swell with a fast wobble, then pop.
            var t = Mathf.Clamp01(age / SwellSeconds);
            var wobble = Mathf.Sin(t * Mathf.PI * 6f) * 0.08f * (1f - t);
            var grow = 1f + 0.35f * t * t;
            swell.localScale = new Vector3(bodySize.x * (grow + wobble), bodySize.y * (grow - wobble), bodySize.z * (grow + wobble));
            swell.localPosition = new Vector3(0f, swell.localScale.y * 0.5f, 0f);
            if (t >= 1f) Burst();
            return;
        }

        var sinceBurst = age - SwellSeconds;
        var fade = Mathf.Clamp01((sinceBurst - SettleSeconds) / FadeSeconds);
        foreach (var blob in blobs) StepBlob(blob, dt, 1f - fade);
        if (fade >= 1f) Destroy(gameObject);
    }

    private void Burst()
    {
        burst = true;
        var center = swell.position;
        Destroy(swell.gameObject);
        PrototypeGameFeel.Shake(0.75f, 0.45f);
        PrototypeGameFeel.Sparks(center, color, 40);
        PrototypeGameFeel.Sparks(center + Vector3.up, new Color(0.75f, 1f, 0.5f), 25);

        var size = Mathf.Max(bodySize.x, bodySize.z);
        var count = 46;
        for (var i = 0; i < count; i++)
        {
            var radius = Random.Range(0.18f, 0.55f) * size / 9f * 1.6f;
            var go = Sphere("Goo_Bit", transform, gooMaterial);
            AddBlob(go.transform, center + Random.insideUnitSphere * size * 0.25f, radius, Random.Range(4f, 13f), Random.Range(5f, 14f));
        }

        // Its two big eyes fly out too.
        for (var i = 0; i < 2; i++)
        {
            var eye = Sphere("Goo_Eye", transform, eyeWhite);
            var pupil = Sphere("Pupil", eye.transform, eyeBlack);
            pupil.transform.localPosition = new Vector3(0f, 0.05f, 0.35f);
            pupil.transform.localScale = new Vector3(0.55f, 0.6f, 0.4f);
            AddBlob(eye.transform, center + Vector3.up * 1.5f, size * 0.07f, Random.Range(5f, 8f), Random.Range(11f, 15f));
        }
    }

    private void AddBlob(Transform blob, Vector3 start, float radius, float outward, float upward)
    {
        blob.position = start;
        blob.localScale = Vector3.one * radius * 2f;
        var direction = Random.insideUnitCircle.normalized;
        blobs.Add(new Blob
        {
            Transform = blob,
            Radius = radius,
            Velocity = new Vector3(direction.x * outward, upward, direction.y * outward)
        });
    }

    private static void StepBlob(Blob blob, float dt, float size)
    {
        if (blob.Transform == null) return;
        if (!blob.Resting)
        {
            blob.Velocity.y += Gravity * dt;
            var position = blob.Transform.position + blob.Velocity * dt;
            if (position.y <= blob.Radius)
            {
                position.y = blob.Radius;
                // Bounce with a splat, losing energy; come to rest when slow.
                blob.Squish = 1f;
                blob.Velocity = new Vector3(blob.Velocity.x * 0.55f, -blob.Velocity.y * 0.38f, blob.Velocity.z * 0.55f);
                if (blob.Velocity.y < 1.2f) blob.Resting = true;
            }
            blob.Transform.position = position;
        }

        blob.Squish = Mathf.MoveTowards(blob.Squish, 0f, dt * 4f);
        var flat = blob.Resting ? 0.35f : 1f - 0.45f * blob.Squish;
        var wide = blob.Resting ? 1.45f : 1f + 0.35f * blob.Squish;
        var diameter = blob.Radius * 2f * size;
        blob.Transform.localScale = new Vector3(diameter * wide, diameter * flat, diameter * wide);
        if (blob.Resting)
        {
            var p = blob.Transform.position;
            blob.Transform.position = new Vector3(p.x, diameter * flat * 0.5f, p.z);
        }
    }

    private static GameObject Sphere(string name, Transform parent, Material material)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        Destroy(sphere.GetComponent<Collider>());
        sphere.transform.SetParent(parent, false);
        sphere.GetComponent<Renderer>().sharedMaterial = material;
        return sphere;
    }
}
