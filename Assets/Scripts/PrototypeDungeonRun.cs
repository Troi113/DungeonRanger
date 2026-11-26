using System.Collections.Generic;
using UnityEngine;

/// <summary>A single continuous run; explored floors remain in memory until restart.</summary>
public sealed class PrototypeDungeonRun : MonoBehaviour
{
    private sealed class FloorState
    {
        public GameObject Root;
        public Vector3 UpPosition;
        public Vector3 DownPosition;
    }

    private PrototypePlayerMovement player;
    private GameObject floorRoot;
    private readonly Dictionary<int, FloorState> floors = new();
    private int runSeed;
    public int Floor { get; private set; }
    public bool Victory { get; private set; }

    public void Initialize(PrototypePlayerMovement ranger)
    {
        player = ranger;
        runSeed = Random.Range(1, 1000000);
        player.GetComponent<PrototypeScrolls>().Initialize(runSeed);
        player.GetComponent<PrototypeInventory>().InitializeCurseRandom(unchecked(runSeed + 137));
        player.GetComponent<PrototypePlayerStats>().InitializeProtectionRandom(unchecked(runSeed + 271));
        Floor = 1;
        EnterFloor(false);
    }

    public void Descend()
    {
        if (Floor >= 5 || player.GetComponent<PrototypePlayerStats>().IsDead) return;
        Floor++;
        EnterFloor(false);
    }

    public void Ascend()
    {
        if (Floor <= 1 || player.GetComponent<PrototypePlayerStats>().IsDead) return;
        Floor--;
        EnterFloor(true);
    }

    public void Restart()
    {
        player.gameObject.SetActive(false);
        Destroy(player.gameObject);
        player = PrototypeBootstrap.CreatePlayer(transform,
            PrototypeBootstrap.CreateMaterial("Prototype Player Material", new Color(0.85f, 0.66f, 0.18f)), Vector3.up);
        GetComponent<PrototypeGameUI>().Initialize(player.gameObject);
        var follow = Camera.main != null ? Camera.main.GetComponent<PrototypeCameraFollow>() : null;
        if (follow != null) follow.Target = player.transform;
        runSeed = Random.Range(1, 1000000);
        player.GetComponent<PrototypeScrolls>().Initialize(runSeed);
        player.GetComponent<PrototypeInventory>().InitializeCurseRandom(unchecked(runSeed + 137));
        player.GetComponent<PrototypePlayerStats>().InitializeProtectionRandom(unchecked(runSeed + 271));
        Floor = 1;
        Victory = false;
        foreach (var state in floors.Values)
        {
            state.Root.SetActive(false);
            Destroy(state.Root);
        }
        floors.Clear();
        floorRoot = null;
        EnterFloor(false);
    }

    private void EnterFloor(bool arrivingFromBelow)
    {
        if (floorRoot != null)
        {
            floorRoot.SetActive(false);
        }

        if (floors.TryGetValue(Floor, out var existing))
        {
            floorRoot = existing.Root;
            MovePlayer(arrivingFromBelow ? existing.DownPosition : existing.UpPosition);
            floorRoot.SetActive(true);
            floorRoot.GetComponent<PrototypeFogOfWar>().RefreshNow();
            return;
        }

        floorRoot = new GameObject($"Generated_Floor_{Floor}");
        floorRoot.transform.SetParent(transform, false);
        var seed = runSeed + Floor * 1000;
        var generator = new PrototypeLevelGenerator();
        var layout = Floor == 5 ? generator.BuildBossArena(floorRoot.transform, seed) : generator.Build(floorRoot.transform, seed, false);

        floors.Add(Floor, new FloorState { Root = floorRoot, UpPosition = layout.PlayerSpawn, DownPosition = layout.ExitSpawn });
        MovePlayer(layout.PlayerSpawn);
        CreateStairs(layout.PlayerSpawn, false);
        if (Floor < 5) CreateStairs(layout.ExitSpawn, true);

        var enemies = new List<PrototypeEnemy>();
        if (Floor < 5)
        {
            foreach (var position in layout.EnemySpawns)
                enemies.Add(CreateEnemy(position, layout, seed + enemies.Count, false));
            var director = floorRoot.AddComponent<PrototypeEnemyWakeDirector>();
            director.Initialize(enemies, seed + 500);
        }
        else
        {
            var goo = CreateEnemy(layout.ExitSpawn, layout, seed + 500, true);
            var bossFloor = floorRoot.transform;
            goo.Defeated += () => PrototypeVictoryAmulet.Create(bossFloor, goo.transform.position, this);
            enemies.Add(goo);
        }
        var fog = floorRoot.AddComponent<PrototypeFogOfWar>();
        fog.Initialize(player, layout.WalkableCells, floorRoot.transform, layout.BlockedEdges);
    }

    public void CollectAmulet()
    {
        if (Victory) return;
        Victory = true;
        PrototypeVictoryScreen.Show();
    }

    private void MovePlayer(Vector3 position)
    {
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.position = new Vector3(position.x, 1f, position.z);
        controller.enabled = true;
    }

    private PrototypeEnemy CreateEnemy(Vector3 position, PrototypeLevelGenerator.Layout layout, int seed, bool boss)
    {
        var obj = GameObject.CreatePrimitive(boss ? PrimitiveType.Sphere : PrimitiveType.Cube);
        obj.name = boss ? "Goo_Boss" : "Enemy_Sleeping";
        obj.transform.SetParent(floorRoot.transform, false);
        obj.transform.position = position;
        obj.transform.localScale = boss ? new Vector3(3f, 2f, 3f) : new Vector3(0.9f, 2f, 0.9f);
        var enemy = obj.AddComponent<PrototypeEnemy>();
        enemy.Initialize(player, layout.WalkableCells, seed, layout.BlockedEdges);
        if (boss) enemy.ConfigureBoss(18);
        return enemy;
    }

    private void CreateStairs(Vector3 position, bool down)
    {
        var stairs = new GameObject(down ? "Stairs_Down" : "Stairs_Up");
        stairs.transform.SetParent(floorRoot.transform, false);
        stairs.transform.position = new Vector3(position.x, 0.02f, position.z);
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { color = down ? new Color(0.15f, 0.7f, 0.85f) : new Color(0.85f, 0.75f, 0.35f) };
        for (var i = 0; i < 4; i++)
        {
            var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = $"Step_{i + 1}";
            step.transform.SetParent(stairs.transform, false);
            step.transform.localPosition = new Vector3(0f, 0.04f + (down ? 3 - i : i) * 0.045f, -0.75f + i * 0.5f);
            step.transform.localScale = new Vector3(2f, 0.08f, 0.5f);
            step.GetComponent<Renderer>().sharedMaterial = material;
            step.GetComponent<Collider>().enabled = false;
        }
        stairs.AddComponent<PrototypeStairs>().Initialize(this, down);
    }
}
