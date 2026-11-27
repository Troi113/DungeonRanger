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

    // Rat models are drawn well beyond their hit box (2x the earlier 1.4x) so they read clearly from the camera.
    private const float ModelLengthScale = 3.5f; // 1.25x the earlier 2.8
    private const string StairsModelPath = "KayKit/Dungeon/stairs";
    // Stairs fill their cell: nearly a tile wide and deep, rising to the top of the walls.
    private const float StairsWidth = PrototypeLevelGenerator.TileSize - 0.1f;
    private const float StairsDepth = PrototypeLevelGenerator.TileSize - 0.4f;
    private const float StairsHeight = 2.5f;
    // Share of floor 5 cells seeded with water before smoothing (lower = less water).
    private const float BossWaterFill = 0.42f;
    // Goo's body radius in world units (its model is much bigger).
    private const float GooBodyRadius = 1.6f;

    [Tooltip("Chance that a generated rat is an albino rat (Pixel Dungeon uses 1 in 30).")]
    [SerializeField, Range(0f, 1f)] private float albinoRatChance = 1f / 30f;

    private PrototypePlayerMovement player;
    private GameObject floorRoot;
    private readonly Dictionary<int, FloorState> floors = new();
    private int runSeed;
    private PrototypeEnemy bossGoo;
    private RectInt bossRoomBounds;
    private bool bossSealed;

    /// <summary>Like Pixel Dungeon, the way back up is sealed once the ranger enters Goo's room, until Goo dies.</summary>
    public bool IsBossSealed => bossSealed && bossGoo != null && bossGoo.IsAlive;

    private void Update()
    {
        if (bossSealed || Floor != 5 || bossGoo == null || !bossGoo.IsAlive || player == null) return;
        if (!bossRoomBounds.Contains(PrototypeLevelGenerator.WorldToGrid(player.transform.position))) return;
        bossSealed = true;
        bossGoo.AlertTo(player);
        PrototypeGameUI.ShowNotice("The way back is sealed!");
    }

    private static RectInt RoomContaining(PrototypeLevelGenerator.Layout layout, Vector2Int cell)
    {
        foreach (var room in layout.RoomBounds) if (room.Contains(cell)) return room;
        return layout.RoomBounds.Count > 0 ? layout.RoomBounds[layout.RoomBounds.Count - 1] : new RectInt(cell.x - 2, cell.y - 2, 5, 5);
    }
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
        player.GetComponent<PrototypePotions>()?.Initialize(runSeed);
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
        PrototypeHeroDeath.RestoreCamera();
        WatchPlayerDeath();
        runSeed = Random.Range(1, 1000000);
        player.GetComponent<PrototypeScrolls>().Initialize(runSeed);
        player.GetComponent<PrototypePotions>()?.Initialize(runSeed);
        Floor = 1;
        Victory = false;
        runEnded = false;
        Score = 0;
        ElapsedSeconds = 0f;
        DeepestFloor = 1;
        bossGoo = null;
        bossSealed = false;
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
        if (!runEnded) PrototypeLeaderboard.Record(Score, DeepestFloor, ElapsedSeconds, Victory);
        runEnded = true;
        PrototypeSaveSystem.Delete();
    }

    // ---------------- Score ----------------

    private const int PointsPerNewFloor = 100;
    private const int VictoryBonus = 1000;
    // Victory time bonus: up to this many points, minus one per second of play.
    private const int MaxTimeBonus = 900;

    public int Score { get; private set; }
    public float ElapsedSeconds { get; private set; }
    public int DeepestFloor { get; private set; } = 1;

    /// <summary>
    /// Pixel Dungeon experience: a rat gives 1 (none once the hero is above level 5, the rat's
    /// max level); Goo gives 10.
    /// </summary>
    private void GrantKillExperience(PrototypeMobs.MobData data)
    {
        var stats = player != null ? player.GetComponent<PrototypePlayerStats>() : null;
        if (stats == null) return;
        var experience = data.Id == "goo" ? 10 : stats.Level <= 5 ? 1 : 0;
        if (stats.EarnExperience(experience) > 0)
        {
            var talents = player.GetComponent<PrototypeTalents>();
            PrototypeGameUI.ShowNotice(talents != null && talents.PointsAvailable > 0
                ? $"Level up! Level {stats.Level}. Talent point ready - press O."
                : $"Level up! You are now level {stats.Level}.");
        }
    }

    private static int KillPoints(PrototypeMobs.MobData data) => data.Id switch
    {
        "goo" => 500,
        "albino_rat" => 30,
        _ => 10
    };

    private void LateUpdate()
    {
        // The run clock counts unpaused play time only (timeScale is 0 while paused).
        if (runEnded || Victory || player == null) return;
        var stats = player.GetComponent<PrototypePlayerStats>();
        if (stats != null && !stats.IsDead) ElapsedSeconds += Time.deltaTime;
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
        if (Floor > DeepestFloor)
        {
            DeepestFloor = Floor;
            Score += PointsPerNewFloor;
        }
        MovePlayer(state.UpPosition);
    }

    /// <summary>Generates a floor from the run seed, applying saved changes when restoring.</summary>
    private FloorState BuildFloor(int floorNumber, PrototypeFloorSave saved)
    {
        floorRoot = new GameObject($"Generated_Floor_{floorNumber}");
        floorRoot.transform.SetParent(transform, false);
        var seed = runSeed + floorNumber * 1000;
        var generator = new PrototypeLevelGenerator();
        // Floor 5 follows Pixel Dungeon's SewerBossLevel: a normal sewer layout (entrance room,
        // tunnels, boss room at the far end) rather than a bare arena.
        var layout = generator.Build(floorRoot.transform, seed, false, floorNumber, runSeed);

        PrototypeDungeonDressing.Apply(floorRoot.transform, seed);
        var state = new FloorState { Root = floorRoot, UpPosition = layout.PlayerSpawn, DownPosition = layout.ExitSpawn };
        floors.Add(floorNumber, state);
        var walkable = new HashSet<Vector2Int>(layout.WalkableCells);
        CreateStairs(layout.PlayerSpawn, false, walkable);
        if (floorNumber < 5) CreateStairs(layout.ExitSpawn, true, walkable);

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
            // Goo lives in the boss room (the room farthest along the generated path) and stays there.
            var bossRoom = RoomContaining(layout, PrototypeLevelGenerator.WorldToGrid(layout.ExitSpawn));
            // Goo starts in its room but can squeeze through corridors to chase the ranger.
            var goo = CreateEnemy(layout.ExitSpawn, layout, seed + 500, true, PrototypeMobs.Goo);
            var bossFloor = floorRoot.transform;
            goo.Defeated += () =>
            {
                bossSealed = false;
                PrototypeGameUI.ShowNotice("Goo is defeated. The way back opens.");
                PrototypeVictoryAmulet.Create(bossFloor, goo.transform.position, this);
            };
            state.Enemies.Add(goo);
            bossGoo = goo;
            bossRoomBounds = bossRoom;
            // Sewer water patches (Goo heals in them); stairs and spawns stay dry. A 40% starting
            // fill smoothed by majority vote leaves scattered pools instead of PD's half-flooded floor.
            PrototypeSewerWater.Create(floorRoot.transform, layout.WalkableCells, seed, BossWaterFill, 5,
                new[] { PrototypeLevelGenerator.WorldToGrid(layout.PlayerSpawn), PrototypeLevelGenerator.WorldToGrid(layout.ExitSpawn) });
        }

        // Saved changes are applied before fog and the wake director collect their enemies.
        if (saved != null) ApplyFloorSave(state, saved);

        if (floorNumber < 5)
        {
            var director = floorRoot.AddComponent<PrototypeEnemyWakeDirector>();
            director.Initialize(state.Enemies, seed + 500);
        }
        // Pixel Dungeon blobs (gas and fire) live on this floor's grid.
        floorRoot.AddComponent<PrototypeBlobs>().Initialize(layout.WalkableCells, layout.BlockedEdges, floorNumber);
        state.Fog = floorRoot.AddComponent<PrototypeFogOfWar>();
        state.Fog.Initialize(player, layout.WalkableCells, floorRoot.transform, layout.BlockedEdges);
        if (saved != null) state.Fog.RestoreExplored(DecodeCells(saved.VisitedCells), DecodeCells(saved.MappedCells));
        return state;
    }

    public void CollectAmulet()
    {
        if (Victory) return;
        Victory = true;
        Score += VictoryBonus + Mathf.Max(0, MaxTimeBonus - Mathf.FloorToInt(ElapsedSeconds));
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
            Strength = stats.Strength,
            Level = stats.Level,
            Experience = stats.Experience,
            MaxHealth = stats.MaxHealth,
            HungerMovingTime = player.GetComponent<PrototypeHunger>()?.MovingTime ?? 0f,
            Score = Score,
            ElapsedSeconds = ElapsedSeconds,
            DeepestFloor = DeepestFloor
        };
        var inventory = player.GetComponent<PrototypeInventory>();
        for (var i = 0; i < inventory.Capacity; i++) data.Inventory.Add(PrototypeItemSave.From(inventory.GetSlot(i)));
        data.KnownScrolls.AddRange(player.GetComponent<PrototypeScrolls>().KnownScrolls);
        var talentRanks = player.GetComponent<PrototypeTalents>();
        if (talentRanks != null) data.TalentRanks.AddRange(talentRanks.Ranks);
        var knownPotions = player.GetComponent<PrototypePotions>();
        if (knownPotions != null) data.KnownPotions.AddRange(knownPotions.KnownPotions);
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
        var potions = player.GetComponent<PrototypePotions>();
        potions?.Initialize(runSeed);
        if (data.KnownPotions != null) potions?.RestoreKnown(data.KnownPotions);
        player.GetComponent<PrototypeTalents>()?.RestoreRanks(data.TalentRanks);

        var inventory = player.GetComponent<PrototypeInventory>();
        for (var i = 0; i < inventory.Capacity; i++)
            inventory.SetSlot(i, i < data.Inventory.Count ? data.Inventory[i].ToItem() : null);
        player.GetComponent<PrototypePlayerStats>().RestoreLevel(data.Level, data.Experience, data.MaxHealth);
        player.GetComponent<PrototypePlayerStats>().RestoreHealth(data.Health);
        if (data.Strength > 0) player.GetComponent<PrototypePlayerStats>().RestoreStrength(data.Strength);
        player.GetComponent<PrototypeHunger>()?.RestoreMovingTime(data.HungerMovingTime);
        Score = data.Score;
        ElapsedSeconds = data.ElapsedSeconds;
        DeepestFloor = Mathf.Max(1, data.DeepestFloor, data.Floor);

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

    private PrototypeEnemy CreateEnemy(Vector3 position, PrototypeLevelGenerator.Layout layout, int seed, bool boss, PrototypeMobs.MobData data,
        IReadOnlyList<Vector2Int> walkableOverride = null)
    {
        var useModel = !boss && !string.IsNullOrEmpty(data.ModelName);
        // Modeled mobs get an unscaled root with a box collider; others are a scaled primitive.
        var obj = useModel ? new GameObject() : GameObject.CreatePrimitive(boss ? PrimitiveType.Sphere : PrimitiveType.Cube);
        obj.name = boss ? "Goo_Boss" : $"Enemy_{data.Name.Replace(' ', '_')}";
        obj.transform.SetParent(floorRoot.transform, false);
        // Regular mobs stand on the floor; their hit box stays above the ranger's arrow height.
        obj.transform.position = new Vector3(position.x, data.Scale.y * 0.5f, position.z);
        if (useModel) obj.AddComponent<BoxCollider>().size = data.Scale;
        else obj.transform.localScale = data.Scale;
        var enemy = obj.AddComponent<PrototypeEnemy>();
        if (useModel)
            enemy.UseModel(PrototypeMobModels.Create(data.ModelName, obj.transform, data.Scale.z * ModelLengthScale, -data.Scale.y * 0.5f));
        enemy.Initialize(player, walkableOverride ?? layout.WalkableCells, seed, layout.BlockedEdges);
        var killFloor = floorRoot.transform;
        enemy.Defeated += () =>
        {
            Score += KillPoints(data);
            GrantKillExperience(data);
            if (player != null) player.GetComponent<PrototypeTalents>()?.OnEnemyKilled(obj.transform.position, killFloor, Floor);
        };
        obj.AddComponent<PrototypeHitFlash>();
        if (boss)
        {
            enemy.ConfigureBoss(data.Health);
            obj.AddComponent<PrototypeGooFace>();
            obj.AddComponent<PrototypeSlimeTrail>();
            obj.AddComponent<PrototypeGooWaterHeal>();
            obj.AddComponent<PrototypeGooGlob>();
            // The big slime model stays; its body for hits and movement is about one tile wide,
            // low enough for arrows, so it fits corridors.
            if (obj.GetComponent<Collider>() is SphereCollider body)
            {
                body.radius = GooBodyRadius / Mathf.Max(data.Scale.x, data.Scale.z);
                body.center = new Vector3(0f, (GooBodyRadius - data.Scale.y * 0.5f) / data.Scale.y, 0f);
            }
            // Never let Goo and the ranger push each other's bodies (that could squeeze the
            // ranger through the floor).
            var playerBody = player != null ? player.GetComponent<CharacterController>() : null;
            if (playerBody != null && obj.TryGetComponent<Collider>(out var gooBody)) Physics.IgnoreCollision(playerBody, gooBody);
        }
        else enemy.Configure(data);
        return enemy;
    }

    private static readonly Vector2Int[] StairDirections = { new(0, -1), new(1, 0), new(-1, 0), new(0, 1) };

    /// <summary>Opens a cell's floor into a dark pit (four walls and a bottom) for descending stairs.</summary>
    private void BuildStairPit(Vector2Int cell, float depth)
    {
        PrototypeDungeonDressing.ClearFloorAt(floorRoot.transform, cell);
        var pit = new GameObject("Stair_Pit").transform;
        pit.SetParent(floorRoot.transform, false);
        var center = PrototypeLevelGenerator.GridToWorld(cell, 0f);
        var size = PrototypeLevelGenerator.TileSize;
        var material = PrototypeBootstrap.CreateMaterial("Stair Pit", new Color(0.05f, 0.06f, 0.07f));
        void Slab(string name, Vector3 offset, Vector3 scale)
        {
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = name;
            slab.transform.SetParent(pit, false);
            slab.transform.position = center + offset;
            slab.transform.localScale = scale;
            slab.GetComponent<Renderer>().sharedMaterial = material;
            slab.GetComponent<Collider>().enabled = false;
        }
        Slab("Pit_Bottom", new Vector3(0f, -depth, 0f), new Vector3(size, 0.1f, size));
        Slab("Pit_North", new Vector3(0f, -depth * 0.5f, size * 0.5f), new Vector3(size, depth, 0.1f));
        Slab("Pit_South", new Vector3(0f, -depth * 0.5f, -size * 0.5f), new Vector3(size, depth, 0.1f));
        Slab("Pit_East", new Vector3(size * 0.5f, -depth * 0.5f, 0f), new Vector3(0.1f, depth, size));
        Slab("Pit_West", new Vector3(-size * 0.5f, -depth * 0.5f, 0f), new Vector3(0.1f, depth, size));
    }

    private void CreateStairs(Vector3 position, bool down, HashSet<Vector2Int> walkable)
    {
        // Stairs extend into a walkable neighboring cell; the stand point stays at the given position.
        var standCell = PrototypeLevelGenerator.WorldToGrid(position);
        // Prefer a neighbor backed by a wall, so the stairs run up into (or down beside) the wall.
        var direction = StairDirections[0];
        var found = false;
        for (var pass = 0; pass < 2 && !found; pass++)
        {
            foreach (var candidate in StairDirections)
            {
                if (!walkable.Contains(standCell + candidate)) continue;
                if (pass == 0 && walkable.Contains(standCell + candidate * 2)) continue;
                direction = candidate;
                found = true;
                break;
            }
        }
        var stairs = new GameObject(down ? "Stairs_Down" : "Stairs_Up");
        stairs.transform.SetParent(floorRoot.transform, false);
        stairs.transform.position = new Vector3(position.x, 0f, position.z);
        // Local -Z points from the stand point toward the stairs' cell.
        stairs.transform.rotation = Quaternion.LookRotation(new Vector3(-direction.x, 0f, -direction.y));
        var glowColor = down ? new Color(0.2f, 0.6f, 1f) : new Color(1f, 0.82f, 0.3f);
        var tile = PrototypeLevelGenerator.TileSize;
        var prefab = PrototypeKayKit.Prefab(StairsModelPath);
        if (prefab != null)
        {
            // KayKit stone stairs in the same damp sewer stone as the floor and walls, filling the
            // neighboring cell edge to edge. The model rises toward its local -Z (its low step is at +Z).
            var model = PrototypeKayKit.Spawn(prefab, stairs.transform, PrototypeDungeonDressing.StoneMaterial).transform;
            var bounds = PrototypeKayKit.LocalBounds(StairsModelPath, prefab);
            var scale = new Vector3(
                StairsWidth / Mathf.Max(0.001f, bounds.size.x),
                StairsHeight / Mathf.Max(0.001f, bounds.size.y),
                StairsDepth / Mathf.Max(0.001f, bounds.size.z));
            model.localScale = scale;
            if (down)
            {
                // Going down: the stairs sink into a pit in the neighboring cell, top step level
                // with the floor at the stand cell's edge, descending away from it.
                model.localRotation = Quaternion.Euler(0f, 180f, 0f);
                model.localPosition = new Vector3(bounds.center.x * scale.x, -bounds.max.y * scale.y, -tile * 0.5f + bounds.min.z * scale.z);
                BuildStairPit(standCell + direction, StairsHeight + 0.2f);
            }
            else
            {
                // Going up: the top step meets the far edge of the cell (the wall), the first step
                // starts at the near edge, on the floor.
                model.localPosition = new Vector3(-bounds.center.x * scale.x, -bounds.min.y * scale.y, -tile * 1.5f + 0.05f - bounds.min.z * scale.z);
            }
        }
        else
        {
            var material = PrototypeDungeonDressing.StoneMaterial;
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
        }
        // The stone itself does not glow; a light and a beam of light rising above the walls mark
        // the stairs, so they can be seen from other rooms. The way down is the brighter beacon.
        var beaconCenter = new Vector3(0f, 0f, -tile);
        PrototypeGlowPulse.Add(stairs, null, glowColor, 0f, down ? 5f : 3.5f, down ? 10f : 8f, beaconCenter + Vector3.up * (down ? 0.8f : 2.2f));
        PrototypeStairsBeacon.Add(stairs.transform, beaconCenter, glowColor, down ? 1f : 0.7f);
        stairs.AddComponent<PrototypeStairs>().Initialize(this, down);
    }
}
