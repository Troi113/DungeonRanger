using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pixel Dungeon "blobs" on the floor grid: gases and fire held as per-cell amounts that evolve
/// on a fixed tick (one PD turn ~ 0.5 s here). One component per generated floor; the floor
/// that is currently enabled is Active.
///   Gas (Blob.evolve): each open cell becomes the average of itself and its open orthogonal
///     neighbors, minus 1, so a cloud spreads out and thins away. Walls between cells block it.
///   Toxic gas: damages characters inside each tick ((max HP + 5 + 5 x floor) / 40, rounded
///     randomly), and where it meets paralytic gas the weaker of the two in that cell is removed.
///   Paralytic gas: paralyzes characters inside while they stay in it.
///   Fire: each burning cell loses 1 per tick and sets anyone in it on fire. Wooden doors are the
///     only flammable things: fire on either side of a doorway sets its door alight.
///   Freezing: puts out fire in a cell (no frost source exists yet).
/// </summary>
public sealed class PrototypeBlobs : MonoBehaviour
{
    public enum Kind { ToxicGas, ParalyticGas, Fire }

    [SerializeField] private float tickSeconds = 0.5f;

    public static PrototypeBlobs Active { get; private set; }

    private const int Size = PrototypeLevelGenerator.Width;
    private readonly int[][] cur = { new int[Size * Size], new int[Size * Size], new int[Size * Size] };
    private readonly int[][] off = { new int[Size * Size], new int[Size * Size], new int[Size * Size] };
    private readonly int[] volume = new int[3];
    private bool[] open;
    private HashSet<long> blockedEdges;
    private int floorNumber = 1;
    private float timer;
    private ParticleSystem[] emitters;
    private Light fireLight;
    private readonly List<PrototypeEnemy> enemyBuffer = new();
    private PrototypeDoor[] doors;

    public void Initialize(IEnumerable<Vector2Int> walkable, IEnumerable<long> blocked, int floor)
    {
        open = new bool[Size * Size];
        foreach (var cell in walkable) if (InGrid(cell)) open[Index(cell)] = true;
        blockedEdges = new HashSet<long>(blocked);
        floorNumber = floor;
        BuildEmitters();
        Active = this;
    }

    private void OnEnable() { if (open != null) Active = this; }
    private void OnDisable() { if (Active == this) Active = null; }

    // ---------------- Seeding ----------------

    public void Seed(Kind kind, Vector3 world, int amount) => Seed(kind, PrototypeLevelGenerator.WorldToGrid(world), amount);

    public void Seed(Kind kind, Vector2Int cell, int amount)
    {
        if (!InGrid(cell) || !open[Index(cell)] || amount <= 0) return;
        var k = (int)kind;
        var i = Index(cell);
        if (kind == Kind.Fire)
        {
            // Fire.seed: only lights a cell that is not already burning.
            if (cur[k][i] > 0) return;
            cur[k][i] = amount;
        }
        else cur[k][i] += amount;
        volume[k] += amount;
    }

    /// <summary>Fire on a cell and its open neighbors (Liquid Flame, fire traps).</summary>
    public void SeedFireArea(Vector3 world, int amount, int radius = 1)
    {
        var center = PrototypeLevelGenerator.WorldToGrid(world);
        for (var dx = -radius; dx <= radius; dx++)
        for (var dy = -radius; dy <= radius; dy++)
            Seed(Kind.Fire, center + new Vector2Int(dx, dy), amount);
    }

    /// <summary>Freezing.affect: puts out fire in a cell.</summary>
    public void Freeze(Vector2Int cell)
    {
        if (!InGrid(cell)) return;
        var i = Index(cell);
        volume[(int)Kind.Fire] -= cur[(int)Kind.Fire][i];
        cur[(int)Kind.Fire][i] = 0;
    }

    public int Amount(Kind kind, Vector3 world)
    {
        var cell = PrototypeLevelGenerator.WorldToGrid(world);
        return InGrid(cell) ? cur[(int)kind][Index(cell)] : 0;
    }

    // ---------------- Ticking ----------------

    private void Update()
    {
        if (open == null || PrototypeGameUI.IsGamePaused) return;
        timer += Time.deltaTime;
        if (timer < tickSeconds) return;
        timer -= tickSeconds;
        if (volume[0] + volume[1] + volume[2] <= 0) { UpdateFireLight(); return; }

        EvolveGas((int)Kind.ToxicGas);
        EvolveGas((int)Kind.ParalyticGas);
        EvolveFire();
        IgniteDoors();
        ToxicCancelsParalytic();
        AffectCharacters();
        EmitVisuals();
        UpdateFireLight();
    }

    private void EvolveGas(int k)
    {
        if (volume[k] <= 0) return;
        var c = cur[k];
        var o = off[k];
        volume[k] = 0;
        for (var y = 0; y < Size; y++)
        for (var x = 0; x < Size; x++)
        {
            var i = x + y * Size;
            if (!open[i]) { o[i] = 0; continue; }
            var count = 1;
            var sum = c[i];
            var cell = new Vector2Int(x, y);
            AddNeighbor(c, cell, new Vector2Int(x - 1, y), ref sum, ref count);
            AddNeighbor(c, cell, new Vector2Int(x + 1, y), ref sum, ref count);
            AddNeighbor(c, cell, new Vector2Int(x, y - 1), ref sum, ref count);
            AddNeighbor(c, cell, new Vector2Int(x, y + 1), ref sum, ref count);
            var value = sum >= count ? sum / count - 1 : 0;
            o[i] = value;
            volume[k] += value;
        }
        cur[k] = o;
        off[k] = c;
    }

    private void AddNeighbor(int[] c, Vector2Int from, Vector2Int to, ref int sum, ref int count)
    {
        if (!InGrid(to)) return;
        var j = Index(to);
        if (!open[j] || blockedEdges.Contains(PrototypeLevelGenerator.EdgeKey(from, to))) return;
        sum += c[j];
        count++;
    }

    private void EvolveFire()
    {
        var k = (int)Kind.Fire;
        if (volume[k] <= 0) return;
        var c = cur[k];
        volume[k] = 0;
        for (var i = 0; i < c.Length; i++)
        {
            if (c[i] <= 0) continue;
            c[i]--;
            volume[k] += c[i];
        }
    }

    /// <summary>Fire next to a wooden door sets it alight.</summary>
    private void IgniteDoors()
    {
        var k = (int)Kind.Fire;
        if (volume[k] <= 0) return;
        doors ??= GetComponentsInChildren<PrototypeDoor>(true);
        foreach (var door in doors)
        {
            if (door == null || door.IsBurning || door.IsBurnt) continue;
            var a = door.Cell;
            var b = door.Cell + door.Direction;
            if ((InGrid(a) && cur[k][Index(a)] > 0) || (InGrid(b) && cur[k][Index(b)] > 0)) door.Ignite();
        }
    }

    private void ToxicCancelsParalytic()
    {
        int t = (int)Kind.ToxicGas, p = (int)Kind.ParalyticGas;
        if (volume[t] <= 0 || volume[p] <= 0) return;
        for (var i = 0; i < cur[t].Length; i++)
        {
            int toxic = cur[t][i], paralytic = cur[p][i];
            if (toxic == 0 || paralytic == 0) continue;
            if (paralytic >= toxic) { volume[t] -= toxic; cur[t][i] = 0; }
            else { volume[p] -= paralytic; cur[p][i] = 0; }
        }
    }

    private void AffectCharacters()
    {
        var player = FindAnyObjectByType<PrototypePlayerMovement>();
        if (player != null)
        {
            var stats = player.GetComponent<PrototypePlayerStats>();
            var statuses = player.GetComponent<PrototypeStatusEffects>();
            var cell = PrototypeLevelGenerator.WorldToGrid(player.transform.position);
            if (InGrid(cell) && stats != null && !stats.IsDead)
            {
                var i = Index(cell);
                if (cur[(int)Kind.ToxicGas][i] > 0) stats.TakeDamage(ToxicDamage(stats.MaxHealth));
                if (cur[(int)Kind.ParalyticGas][i] > 0) statuses?.Apply(PrototypeStatusEffectType.Paralyzed, tickSeconds * 2f);
                if (cur[(int)Kind.Fire][i] > 0) statuses?.Ignite();
            }
        }

        enemyBuffer.Clear();
        enemyBuffer.AddRange(FindObjectsByType<PrototypeEnemy>(FindObjectsInactive.Exclude));
        foreach (var enemy in enemyBuffer)
        {
            if (enemy == null || !enemy.IsAlive) continue;
            var cell = PrototypeLevelGenerator.WorldToGrid(enemy.transform.position);
            if (!InGrid(cell)) continue;
            var i = Index(cell);
            if (cur[(int)Kind.ParalyticGas][i] > 0) enemy.Paralyze(tickSeconds * 2f);
            var damage = 0;
            if (cur[(int)Kind.ToxicGas][i] > 0)
            {
                // Goo resists toxic gas (Pixel Dungeon), taking half.
                damage += ToxicDamage(enemy.MaxHealth);
                if (enemy.IsBoss) damage /= 2;
            }
            if (cur[(int)Kind.Fire][i] > 0) damage += 1;
            if (damage > 0) enemy.TakeDamage(damage);
        }
    }

    private int ToxicDamage(int maxHealth)
    {
        var total = maxHealth + 5 + floorNumber * 5;
        return total / 40 + (Random.Range(0, 40) < total % 40 ? 1 : 0);
    }

    // ---------------- Visuals ----------------

    private void BuildEmitters()
    {
        emitters = new ParticleSystem[3];
        emitters[0] = MakeEmitter("Toxic_Gas", new Color(0.45f, 0.85f, 0.25f, 0.35f), 2.6f, 1.4f, 0.4f);
        emitters[1] = MakeEmitter("Paralytic_Gas", new Color(1f, 0.85f, 0.35f, 0.35f), 2.6f, 1.4f, 0.4f);
        emitters[2] = MakeEmitter("Fire", new Color(1f, 0.55f, 0.15f, 0.9f), 1.1f, 0.6f, 2.5f);
        fireLight = new GameObject("Fire_Light").AddComponent<Light>();
        fireLight.transform.SetParent(transform, false);
        fireLight.type = LightType.Point;
        fireLight.color = new Color(1f, 0.55f, 0.2f);
        fireLight.shadows = LightShadows.None;
        fireLight.enabled = false;
    }

    private ParticleSystem MakeEmitter(string name, Color color, float size, float life, float rise)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(transform, false);
        var particles = obj.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = life;
        main.startSize = size;
        main.startColor = color;
        main.startSpeed = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 4000;
        var emission = particles.emission;
        emission.enabled = false;
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(rise);
        var alpha = particles.colorOverLifetime;
        alpha.enabled = true;
        alpha.color = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) }
        };
        obj.GetComponent<ParticleSystemRenderer>().sharedMaterial = TransparentParticleMaterial(name);
        particles.Play();
        return particles;
    }

    private static Material TransparentParticleMaterial(string name)
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        var material = new Material(shader) { name = name };
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

    private void EmitVisuals()
    {
        var tile = PrototypeLevelGenerator.TileSize;
        for (var k = 0; k < 3; k++)
        {
            if (volume[k] <= 0) continue;
            var c = cur[k];
            for (var i = 0; i < c.Length; i++)
            {
                if (c[i] <= 0) continue;
                var center = PrototypeLevelGenerator.GridToWorld(new Vector2Int(i % Size, i / Size), 0f);
                var count = k == (int)Kind.Fire ? 4 : Mathf.Clamp(1 + c[i] / 25, 1, 3);
                for (var n = 0; n < count; n++)
                {
                    var p = new ParticleSystem.EmitParams
                    {
                        position = center + new Vector3(Random.Range(-0.45f, 0.45f) * tile, Random.Range(0.3f, k == (int)Kind.Fire ? 0.8f : 2.2f), Random.Range(-0.45f, 0.45f) * tile),
                        applyShapeToPosition = false
                    };
                    emitters[k].Emit(p, 1);
                }
            }
        }
    }

    private void UpdateFireLight()
    {
        var c = cur[(int)Kind.Fire];
        Vector3 sum = Vector3.zero;
        var count = 0;
        for (var i = 0; i < c.Length; i++)
        {
            if (c[i] <= 0) continue;
            sum += PrototypeLevelGenerator.GridToWorld(new Vector2Int(i % Size, i / Size), 1.5f);
            count++;
        }
        fireLight.enabled = count > 0;
        if (count == 0) return;
        fireLight.transform.position = sum / count;
        fireLight.range = 6f + count * 1.5f;
        fireLight.intensity = 3f + Random.Range(-0.4f, 0.4f);
    }

    private static bool InGrid(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < Size && cell.y < Size;
    private static int Index(Vector2Int cell) => cell.x + cell.y * Size;
}
