using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A single continuous run; explored floors remain in memory until restart.
/// Quitting saves the run (seed, player, and each visited floor's changes); death or victory deletes it.
/// </summary>
public sealed class PrototypeDungeonRun : MonoBehaviour
{
    private sealed class FloorState
    {
        public GameObject Root;
        public Vector3 UpPosition;
        public Vector3 DownPosition;
        public readonly List<PrototypeEnemy> Enemies = new();
        public PrototypeItemPickup[] GeneratedPickups;
        public PrototypeTrap[] Traps;
        public PrototypeChest Chest;
        public PrototypeFogOfWar Fog;
    }

    [Tooltip("Chance that a generated rat is an albino rat (Pixel Dungeon uses 1 in 30).")]
    [SerializeField, Range(0f, 1f)] private float albinoRatChance = 1f / 30f;

    private PrototypePlayerMovement player;
    private GameObject floorRoot;
    private readonly Dictionary<int, FloorState> floors = new();
    private int runSeed;
    private bool runEnded;
    public int Floor { get; private set; }
    public bool Victory { get; private set; }
    /// <summary>Parent for items dropped or thrown on the current floor.</summary>
    public Transform CurrentFloorRoot => floorRoot != null ? floorRoot.transform : transform;

    public void Initialize(PrototypePlayerMovement ranger)
    {
        player = ranger;
        WatchPlayerDeath();
        var saved = PrototypeSaveSystem.ConsumeContinue();
        if (saved != null && RestoreRun(saved)) return;

        runSeed = Random.Range(1, 1000000);
        player.GetComponent<PrototypeScrolls>().Initialize(runSeed);
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
        WatchPlayerDeath();
        runSeed = Random.Range(1, 1000000);
        player.GetComponent<PrototypeScrolls>().Initialize(runSeed);
        Floor = 1;
        Victory = false;
        runEnded = false;
        ClearFloors();
        EnterFloor(false);
    }

    private void ClearFloors()
    {
        foreach (var state in floors.Values)
        {
            state.Root.SetActive(false);
            Destroy(state.Root);
        }
        floors.Clear();
        floorRoot = null;
    }

    private void WatchPlayerDeath()
    {
        var stats = player.GetComponent<PrototypePlayerStats>();
        if (stats != null) stats.Died += EndRun;
    }

    /// <summary>A finished run (death or victory) leaves no game in progress.</summary>
    private void EndRun()
    {
        runEnded = true;
        PrototypeSaveSystem.Delete();
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
            existing.Fog.RefreshNow();
            return;
        }

        var state = BuildFloor(Floor, null);
        MovePlayer(state.UpPosition);
    }

    /// <summary>Generates a floor from the run seed, applying saved changes when restoring.</summary>
    private FloorState BuildFloor(int floorNumber, PrototypeFloorSave saved)
    {
        floorRoot = new GameObject($"Generated_Floor_{floorNumber}");
        floorRoot.transform.SetParent(transform, false);
        var seed = runSeed + floorNumber * 1000;
        var generator = new PrototypeLevelGenerator();
        var layout = floorNumber == 5 ? generator.BuildBossArena(floorRoot.transform, seed) : generator.Build(floorRoot.transform, seed, false);

        var state = new FloorState { Root = floorRoot, UpPosition = layout.PlayerSpawn, DownPosition = layout.ExitSpawn };
        floors.Add(floorNumber, state);
        CreateStairs(layout.PlayerSpawn, false);
        if (floorNumber < 5) CreateStairs(layout.ExitSpawn, true);

        // Generation is deterministic, so creation order identifies objects across save/load.
        state.GeneratedPickups = floorRoot.GetComponentsInChildren<PrototypeItemPickup>(true);
        for (var i = 0; i < state.GeneratedPickups.Length; i++) state.GeneratedPickups[i].GeneratedIndex = i;
        state.Traps = floorRoot.GetComponentsInChildren<PrototypeTrap>(true);
        state.Chest = floorRoot.GetComponentInChildren<PrototypeChest>(true);

        if (floorNumber < 5)
        {
            var mobRandom = new System.Random(seed + 77);
            foreach (var position in layout.EnemySpawns)
            {
                var data = mobRandom.NextDouble() < albinoRatChance ? PrototypeMobs.AlbinoRat : PrototypeMobs.Rat;
                state.Enemies.Add(CreateEnemy(position, layout, seed + state.Enemies.Count, false, data));
            }
        }
        else
        {
            var goo = CreateEnemy(layout.ExitSpawn, layout, seed + 500, true, PrototypeMobs.Goo);
            var bossFloor = floorRoot.transform;
            goo.Defeated += () => PrototypeVictoryAmulet.Create(bossFloor, goo.transform.position, this);
            state.Enemies.Add(goo);
        }

        // Saved changes are applied before fog and the wake director collect their enemies.
        if (saved != null) ApplyFloorSave(state, saved);

        if (floorNumber < 5)
        {
            var director = floorRoot.AddComponent<PrototypeEnemyWakeDirector>();
            director.Initialize(state.Enemies, seed + 500);
        }
        state.Fog = floorRoot.AddComponent<PrototypeFogOfWar>();
        state.Fog.Initialize(player, layout.WalkableCells, floorRoot.transform, layout.BlockedEdges);
        if (saved != null) state.Fog.RestoreExplored(DecodeCells(saved.VisitedCells), DecodeCells(saved.MappedCells));
        return state;
    }

    public void CollectAmulet()
    {
        if (Victory) return;
        Victory = true;
        EndRun();
        PrototypeVictoryScreen.Show();
    }

    // ---------------- Saving ----------------

    /// <summary>Saves the run in progress; a dead or finished run is deleted instead.</summary>
    public void SaveRun()
    {
        var stats = player != null ? player.GetComponent<PrototypePlayerStats>() : null;
        if (runEnded || Victory || stats == null || stats.IsDead)
        {
            PrototypeSaveSystem.Delete();
            return;
        }

        foreach (var thrown in GetComponentsInChildren<PrototypeThrownItem>()) thrown.LandNow();

        var data = new PrototypeSaveData
        {
            RunSeed = runSeed,
            Floor = Floor,
            PlayerX = player.transform.position.x,
            PlayerZ = player.transform.position.z,
            Health = stats.CurrentHealth,
            HungerMovingTime = player.GetComponent<PrototypeHunger>()?.MovingTime ?? 0f
        };
        var inventory = player.GetComponent<PrototypeInventory>();
        for (var i = 0; i < inventory.Capacity; i++) data.Inventory.Add(PrototypeItemSave.From(inventory.GetSlot(i)));
        data.KnownScrolls.AddRange(player.GetComponent<PrototypeScrolls>().KnownScrolls);
        foreach (var pair in floors) data.Floors.Add(CaptureFloor(pair.Key, pair.Value));
        PrototypeSaveSystem.Save(data);
    }

    private void OnApplicationQuit()
    {
        if (player != null) SaveRun();
    }

    private static PrototypeFloorSave CaptureFloor(int floorNumber, FloorState state)
    {
        var save = new PrototypeFloorSave { Floor = floorNumber };
        for (var i = 0; i < state.GeneratedPickups.Length; i++)
            if (state.GeneratedPickups[i] == null || !state.GeneratedPickups[i].gameObject.activeSelf)
                save.TakenGeneratedPickups.Add(i);

        foreach (var pickup in state.Root.GetComponentsInChildren<PrototypeItemPickup>(true))
        {
            if (pickup.GeneratedIndex >= 0 || !pickup.gameObject.activeSelf || pickup.Item == null) continue;
            save.LooseItems.Add(new PrototypeLooseItemSave
            {
                Item = PrototypeItemSave.From(pickup.Item),
                X = pickup.transform.position.x,
                Z = pickup.transform.position.z
            });
        }

        foreach (var enemy in state.Enemies)
        {
            var alive = enemy != null && enemy.IsAlive;
            save.Enemies.Add(new PrototypeEnemySave
            {
                Alive = alive,
                Health = alive ? enemy.CurrentHealth : 0,
                X = alive ? enemy.transform.position.x : 0f,
                Z = alive ? enemy.transform.position.z : 0f,
                Awake = alive && enemy.IsAwake
            });
        }

        for (var i = 0; i < state.Traps.Length; i++)
        {
            if (state.Traps[i].IsActivated) save.ActivatedTraps.Add(i);
            if (state.Traps[i].WasRevealed) save.RevealedTraps.Add(i);
        }

        save.ChestOpened = state.Chest != null && state.Chest.IsOpened;
        save.VisitedCells = EncodeCells(state.Fog.VisitedCells);
        save.MappedCells = EncodeCells(state.Fog.MappedCells);

        var amulet = state.Root.GetComponentInChildren<PrototypeVictoryAmulet>(true);
        if (amulet != null && amulet.gameObject.activeSelf)
        {
            save.AmuletPresent = true;
            save.AmuletX = amulet.transform.position.x;
            save.AmuletZ = amulet.transform.position.z;
        }
        return save;
    }

    // ---------------- Restoring ----------------

    private bool RestoreRun(PrototypeSaveData data)
    {
        if (data.Floors == null || data.Floors.Count == 0) return false;

        runSeed = data.RunSeed;
        var scrolls = player.GetComponent<PrototypeScrolls>();
        scrolls.Initialize(runSeed);
        scrolls.RestoreKnown(data.KnownScrolls);

        var inventory = player.GetComponent<PrototypeInventory>();
        for (var i = 0; i < inventory.Capacity; i++)
            inventory.SetSlot(i, i < data.Inventory.Count ? data.Inventory[i].ToItem() : null);
        player.GetComponent<PrototypePlayerStats>().RestoreHealth(data.Health);
        player.GetComponent<PrototypeHunger>()?.RestoreMovingTime(data.HungerMovingTime);

        foreach (var floorSave in data.Floors)
        {
            if (floors.ContainsKey(floorSave.Floor)) continue;
            BuildFloor(floorSave.Floor, floorSave);
            floorRoot.SetActive(false);
        }

        Floor = Mathf.Clamp(data.Floor, 1, 5);
        if (!floors.TryGetValue(Floor, out var current))
        {
            current = BuildFloor(Floor, null);
            floorRoot.SetActive(false);
        }
        floorRoot = current.Root;
        floorRoot.SetActive(true);
        MovePlayer(new Vector3(data.PlayerX, 1f, data.PlayerZ));
        current.Fog.RefreshNow();
        return true;
    }

    private void ApplyFloorSave(FloorState state, PrototypeFloorSave saved)
    {
        foreach (var index in saved.TakenGeneratedPickups)
        {
            if (index < 0 || index >= state.GeneratedPickups.Length) continue;
            // Deactivate first so later searches this frame skip it.
            state.GeneratedPickups[index].gameObject.SetActive(false);
            Destroy(state.GeneratedPickups[index].gameObject);
        }

        for (var i = 0; i < state.Enemies.Count && i < saved.Enemies.Count; i++)
        {
            var enemy = state.Enemies[i];
            var enemySave = saved.Enemies[i];
            if (enemySave.Alive)
            {
                enemy.RestoreState(enemySave.Health, new Vector3(enemySave.X, 0f, enemySave.Z), enemySave.Awake);
                continue;
            }
            enemy.gameObject.SetActive(false);
            Destroy(enemy.gameObject);
        }
        state.Enemies.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeSelf);

        for (var i = 0; i < state.Traps.Length; i++)
            state.Traps[i].RestoreState(saved.ActivatedTraps.Contains(i), saved.RevealedTraps.Contains(i));

        if (saved.ChestOpened && state.Chest != null) state.Chest.RestoreOpened();

        foreach (var loose in saved.LooseItems)
        {
            var item = loose.Item?.ToItem();
            if (item == null) continue;
            var pickup = PrototypeItemPickup.CreateForItem(state.Root.transform, new Vector3(loose.X, 0f, loose.Z), item);
            HoldDoorOpenIfInDoorway(state.Root.transform, pickup);
        }

        if (saved.AmuletPresent)
            PrototypeVictoryAmulet.Create(state.Root.transform, new Vector3(saved.AmuletX, 0f, saved.AmuletZ), this);
    }

    /// <summary>An item resting in a doorway keeps that door open.</summary>
    public static void HoldDoorOpenIfInDoorway(Transform floor, PrototypeItemPickup pickup)
    {
        foreach (var door in floor.GetComponentsInChildren<PrototypeDoor>(true))
        {
            var offset = door.transform.position - pickup.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.25f) door.OpenAndHold(pickup.gameObject);
        }
    }

    private static List<int> EncodeCells(IReadOnlyCollection<Vector2Int> cells)
    {
        var encoded = new List<int>(cells.Count);
        foreach (var cell in cells) encoded.Add(cell.y * 1000 + cell.x);
        return encoded;
    }

    private static IEnumerable<Vector2Int> DecodeCells(List<int> encoded)
    {
        if (encoded == null) yield break;
        foreach (var value in encoded) yield return new Vector2Int(value % 1000, value / 1000);
    }

    private void MovePlayer(Vector3 position)
    {
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.position = new Vector3(position.x, 1f, position.z);
        controller.enabled = true;
    }

    private PrototypeEnemy CreateEnemy(Vector3 position, PrototypeLevelGenerator.Layout layout, int seed, bool boss, PrototypeMobs.MobData data)
    {
        var obj = GameObject.CreatePrimitive(boss ? PrimitiveType.Sphere : PrimitiveType.Cube);
        obj.name = boss ? "Goo_Boss" : $"Enemy_{data.Name.Replace(' ', '_')}";
        obj.transform.SetParent(floorRoot.transform, false);
        // Regular mobs stand on the floor; their height stays above the ranger's arrow height.
        obj.transform.position = boss ? position : new Vector3(position.x, data.Scale.y * 0.5f, position.z);
        obj.transform.localScale = data.Scale;
        var enemy = obj.AddComponent<PrototypeEnemy>();
        enemy.Initialize(player, layout.WalkableCells, seed, layout.BlockedEdges);
        if (boss) enemy.ConfigureBoss(data.Health);
        else enemy.Configure(data);
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
