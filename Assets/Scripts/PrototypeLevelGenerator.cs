using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// First full-floor procedural level test.
///
/// The layout is represented as a 32x32 grid. Rooms are placed from a seeded
/// random sequence, then connected by one-cell-wide corridors. This is a small
/// Unity prototype of the room/corridor approach used by Pixel Dungeon.
/// </summary>
public sealed class PrototypeLevelGenerator
{
    public const int Width = 32;
    public const int Height = 32;
    public const int Seed = 4582;

    // A logical dungeon tile is four Unity world units wide. The grid remains
    // 32x32, but rooms and one-tile corridors now have enough space for live
    // movement and ranged combat.
    public const float TileSize = 4f;
    private const float WallThickness = 0.25f;
    private const float WallHeight = 2.5f;
    private const float DoorWidth = 3f;
    private const float DoorHeight = 2.2f;
    private const float DoorFrameThickness = 0.12f;
    private const int DesiredRooms = 6;
    // Rooms may touch; a connection between adjacent rooms uses one door.
    private const int RoomPadding = 0;
    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down
    };

    private static readonly Color FloorColor = new(0.24f, 0.28f, 0.34f);
    private static readonly Color WallColor = new(0.12f, 0.15f, 0.2f);
    private static readonly Color DoorColor = new(0.48f, 0.27f, 0.12f);

    private readonly struct DoorEdge : IEquatable<DoorEdge>
    {
        public readonly Vector2Int Cell;
        public readonly Vector2Int Direction;

        public DoorEdge(Vector2Int cell, Vector2Int direction)
        {
            Cell = cell;
            Direction = direction;
        }

        public bool Equals(DoorEdge other) => Cell == other.Cell && Direction == other.Direction;
        public override bool Equals(object obj) => obj is DoorEdge other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Cell, Direction);
    }

    private readonly struct DoorPlacement
    {
        public readonly Vector2Int Cell;
        public readonly Vector2Int Direction;
        public readonly RectInt OwnerBounds;

        public DoorPlacement(Vector2Int cell, Vector2Int direction, RectInt ownerBounds)
        {
            Cell = cell;
            Direction = direction;
            OwnerBounds = ownerBounds;
        }
    }

    private readonly struct Room
    {
        public readonly RectInt Bounds;

        public Room(RectInt bounds)
        {
            Bounds = bounds;
        }

        public Vector2Int Center => new(Bounds.xMin + Bounds.width / 2, Bounds.yMin + Bounds.height / 2);
    }

    private sealed class FloorPlan
    {
        public bool[,] Occupied;
        public List<Room> Rooms;
        public List<DoorPlacement> Doors;
    }

    public sealed class Layout
    {
        public Vector3 PlayerSpawn;
        public Vector3 ExitSpawn;
        public readonly List<Vector3> EnemySpawns = new();
        public readonly List<Vector2Int> WalkableCells = new();
        public readonly List<RectInt> RoomBounds = new();
        public readonly HashSet<long> BlockedEdges = new();
    }

    public Layout Build(Transform root, int seed = Seed, bool showExitMarker = true)
    {
        var plan = GenerateFloorPlan(seed);
        var occupied = plan.Occupied;
        var rooms = plan.Rooms;
        var doorPlacements = plan.Doors;

        var floorRoot = CreateChild(root, "Level_Floor");
        var wallRoot = CreateChild(root, "Level_Walls");
        var doorRoot = CreateChild(root, "Level_Doors");
        var floorMaterial = CreateMaterial("Level Floor Material", FloorColor);
        var wallMaterial = CreateMaterial("Level Wall Material", WallColor);
        var doorMaterial = CreateMaterial("Level Door Material", DoorColor);
        var connectedDoors = KeepConnectedDoors(occupied, doorPlacements);
        var blockedEdges = FindBlockedRoomEdges(occupied, rooms, connectedDoors);

        BuildFloor(floorRoot, occupied, floorMaterial);
        BuildWalls(wallRoot, occupied, connectedDoors, wallMaterial, blockedEdges);
        BuildDoors(doorRoot, connectedDoors, doorMaterial, seed);
        BuildTraps(root, occupied, seed);
        BuildItemsAndChests(root, rooms, seed);

        var entranceCell = rooms[0].Center;
        var exitCell = rooms[rooms.Count - 1].Center;
        foreach (var room in rooms)
        {
            if (!ValidateConnectivity(occupied, entranceCell, room.Center, blockedEdges))
                Debug.LogError($"Level seed {seed} produced an unreachable room.");
        }

        if (showExitMarker) BuildExitMarker(root, exitCell);

        var layout = new Layout
        {
            PlayerSpawn = GridToWorld(entranceCell, 1f),
            ExitSpawn = GridToWorld(exitCell, 0f)
        };
        layout.BlockedEdges.UnionWith(blockedEdges);
        foreach (var room in rooms) layout.RoomBounds.Add(room.Bounds);

        for (var i = 1; i < rooms.Count; i++)
        {
            layout.EnemySpawns.Add(GridToWorld(rooms[i].Center, 1f));
        }

        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                if (occupied[x, y])
                {
                    layout.WalkableCells.Add(new Vector2Int(x, y));
                }
            }
        }

        return layout;
    }

    private static FloorPlan GenerateFloorPlan(int seed)
    {
        // Retry the geometry before building any Unity objects. This preserves
        // the requested loop even for a crowded or enclosed random placement.
        for (var attempt = 0; attempt < 24; attempt++)
        {
            var occupied = new bool[Width, Height];
            var rooms = CreateRooms(occupied, unchecked(seed + attempt * 7919));
            var doors = new List<DoorPlacement>();
            if (rooms.Count == DesiredRooms && ConnectRoomNetwork(rooms, occupied, doors))
                return new FloorPlan { Occupied = occupied, Rooms = rooms, Doors = doors };
        }

        // A deterministic, well-spaced fallback ensures a playable loop even
        // when all random attempts fail.
        var fallback = new FloorPlan { Occupied = new bool[Width, Height], Rooms = new List<Room>(), Doors = new List<DoorPlacement>() };
        foreach (var y in new[] { 2, 18 })
        foreach (var x in new[] { 2, 13, 24 })
        {
            var room = new Room(new RectInt(x, y, 6, 6));
            fallback.Rooms.Add(room);
            CarveRoom(room.Bounds, fallback.Occupied);
        }
        if (!ConnectRoomNetwork(fallback.Rooms, fallback.Occupied, fallback.Doors))
            Debug.LogError("Failed to generate the fallback dungeon layout.");
        return fallback;
    }

    public Layout BuildBossArena(Transform root, int seed)
    {
        var random = new System.Random(seed);
        var width = random.Next(10, 14);
        var height = random.Next(10, 14);
        var bounds = new RectInt(random.Next(4, Width - width - 4), random.Next(4, Height - height - 4), width, height);
        var occupied = new bool[Width, Height];
        CarveRoom(bounds, occupied);
        BuildFloor(CreateChild(root, "Level_Floor"), occupied, CreateMaterial("Boss Floor", FloorColor));
        BuildWalls(CreateChild(root, "Level_Walls"), occupied, new List<DoorPlacement>(), CreateMaterial("Boss Walls", WallColor));
        var layout = new Layout
        {
            PlayerSpawn = GridToWorld(new Vector2Int(bounds.xMin + 2, bounds.yMin + 2), 1f),
            ExitSpawn = GridToWorld(new Vector2Int(bounds.xMax - 3, bounds.yMax - 3), 1f)
        };
        for (var x = bounds.xMin; x < bounds.xMax; x++)
            for (var y = bounds.yMin; y < bounds.yMax; y++)
                layout.WalkableCells.Add(new Vector2Int(x, y));
        return layout;
    }

    private static List<Room> CreateRooms(bool[,] occupied, int seed)
    {
        var random = new System.Random(seed);
        var rooms = new List<Room>();

        for (var attempt = 0; attempt < 100 && rooms.Count < DesiredRooms; attempt++)
        {
            var width = random.Next(5, 10);
            var height = random.Next(5, 10);
            var x = random.Next(1, Width - width - 1);
            var y = random.Next(1, Height - height - 1);
            var candidate = new RectInt(x, y, width, height);

            var padded = new RectInt(
                candidate.xMin - RoomPadding,
                candidate.yMin - RoomPadding,
                candidate.width + RoomPadding * 2,
                candidate.height + RoomPadding * 2);

            var overlaps = false;
            foreach (var room in rooms)
            {
                if (padded.Overlaps(room.Bounds))
                {
                    overlaps = true;
                    break;
                }
            }

            if (overlaps)
            {
                continue;
            }

            rooms.Add(new Room(candidate));
            CarveRoom(candidate, occupied);
        }

        return rooms;
    }

    private static bool ConnectRoomNetwork(List<Room> rooms, bool[,] occupied, List<DoorPlacement> doors)
    {
        if (rooms.Count < 3) return false;
        var connected = new HashSet<int> { 0 };
        var roomLinks = new HashSet<long>();
        while (connected.Count < rooms.Count)
        {
            List<Vector2Int> shortest = null;
            var bestFrom = -1;
            var bestTo = -1;
            for (var from = 0; from < rooms.Count; from++)
            {
                if (!connected.Contains(from)) continue;
                for (var to = 0; to < rooms.Count; to++)
                {
                    if (connected.Contains(to)) continue;
                    var route = FindRoomRoute(from, to, rooms, occupied, false, doors);
                    if (route == null || (shortest != null && route.Count >= shortest.Count)) continue;
                    shortest = route;
                    bestFrom = from;
                    bestTo = to;
                }
            }

            if (shortest == null)
            {
                return false;
            }
            CarveConnection(shortest, rooms[bestFrom], rooms[bestTo], occupied, doors);
            connected.Add(bestTo);
            roomLinks.Add(RoomLink(bestFrom, bestTo));
        }

        // Add a separate route between two rooms already in the network.
        // Keeping it off existing corridors makes it a real alternate path.
        List<Vector2Int> loop = null;
        var loopFrom = -1;
        var loopTo = -1;
        for (var from = 0; from < rooms.Count; from++)
        for (var to = from + 1; to < rooms.Count; to++)
        {
            if (roomLinks.Contains(RoomLink(from, to))) continue;
            var route = FindRoomRoute(from, to, rooms, occupied, true, doors);
            if (route == null || (loop != null && route.Count >= loop.Count)) continue;
            loop = route;
            loopFrom = from;
            loopTo = to;
        }
        if (loop == null) return false;
        CarveConnection(loop, rooms[loopFrom], rooms[loopTo], occupied, doors);
        return true;
    }

    private static long RoomLink(int first, int second) => ((long)Math.Min(first, second) << 32) | (uint)Math.Max(first, second);

    private static List<Vector2Int> FindRoomRoute(int from, int to, List<Room> rooms, bool[,] occupied, bool avoidCorridors, List<DoorPlacement> doors)
    {
        var queue = new Queue<Vector2Int>();
        var visited = new bool[Width, Height];
        var previous = new Vector2Int[Width, Height];
        var bounds = rooms[from].Bounds;
        var targetBounds = rooms[to].Bounds;

        // Start only on room edges, excluding corners, so openings face a
        // corridor or another room instead of slicing across room interiors.
        for (var x = bounds.xMin; x < bounds.xMax; x++)
        for (var y = bounds.yMin; y < bounds.yMax; y++)
        {
            var cell = new Vector2Int(x, y);
            foreach (var direction in CardinalDirections)
            {
                var next = cell + direction;
                if (bounds.Contains(next) || !IsValidDoorCell(bounds, cell, direction)) continue;
                if (targetBounds.Contains(next) && IsValidDoorCell(targetBounds, next, -direction))
                {
                    if (avoidCorridors && HasDoor(doors, cell, next)) continue;
                    return new List<Vector2Int> { cell, next };
                }
                if (!InsideMap(next) || RoomIndex(rooms, next) >= 0 || visited[next.x, next.y] ||
                    (avoidCorridors && occupied[next.x, next.y])) continue;
                visited[next.x, next.y] = true;
                previous[next.x, next.y] = cell;
                queue.Enqueue(next);
            }
        }

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            foreach (var direction in CardinalDirections)
            {
                var next = cell + direction;
                if (targetBounds.Contains(next) && IsValidDoorCell(targetBounds, next, -direction))
                {
                    var route = new List<Vector2Int> { next };
                    var step = cell;
                    while (!bounds.Contains(step))
                    {
                        route.Add(step);
                        step = previous[step.x, step.y];
                    }
                    route.Add(step);
                    route.Reverse();
                    return route;
                }
                if (!InsideMap(next) || visited[next.x, next.y] || RoomIndex(rooms, next) >= 0 ||
                    (avoidCorridors && occupied[next.x, next.y])) continue;
                visited[next.x, next.y] = true;
                previous[next.x, next.y] = cell;
                queue.Enqueue(next);
            }
        }
        return null;
    }

    private static bool IsValidDoorCell(RectInt room, Vector2Int cell, Vector2Int outward)
    {
        return outward.x != 0 ? cell.y > room.yMin && cell.y < room.yMax - 1 : cell.x > room.xMin && cell.x < room.xMax - 1;
    }

    private static bool InsideMap(Vector2Int cell) => cell.x > 0 && cell.x < Width - 1 && cell.y > 0 && cell.y < Height - 1;

    private static bool HasDoor(List<DoorPlacement> doors, Vector2Int a, Vector2Int b)
    {
        var edge = EdgeKey(a, b);
        foreach (var door in doors)
            if (EdgeKey(door.Cell, door.Cell + door.Direction) == edge) return true;
        return false;
    }

    private static void CarveConnection(List<Vector2Int> route, Room from, Room to, bool[,] occupied, List<DoorPlacement> doors)
    {
        foreach (var cell in route) CarveCell(occupied, cell.x, cell.y);
        doors.Add(new DoorPlacement(route[0], route[1] - route[0], from.Bounds));
        if (route.Count > 2)
        {
            var last = route.Count - 1;
            doors.Add(new DoorPlacement(route[last], route[last - 1] - route[last], to.Bounds));
        }
    }

    private static int RoomIndex(List<Room> rooms, Vector2Int cell)
    {
        for (var i = 0; i < rooms.Count; i++)
            if (rooms[i].Bounds.Contains(cell)) return i;
        return -1;
    }

    public static long EdgeKey(Vector2Int first, Vector2Int second)
    {
        var a = first.x + first.y * Width;
        var b = second.x + second.y * Width;
        return ((long)Math.Min(a, b) << 32) | (uint)Math.Max(a, b);
    }

    private static HashSet<long> FindBlockedRoomEdges(bool[,] occupied, List<Room> rooms, List<DoorPlacement> doors)
    {
        var doorEdges = new HashSet<long>();
        foreach (var door in doors) doorEdges.Add(EdgeKey(door.Cell, door.Cell + door.Direction));
        var blocked = new HashSet<long>();
        var directions = new[] { Vector2Int.right, Vector2Int.up };
        for (var x = 0; x < Width; x++)
        for (var y = 0; y < Height; y++)
        {
            if (!occupied[x, y]) continue;
            var cell = new Vector2Int(x, y);
            foreach (var direction in directions)
            {
                var next = cell + direction;
                if (!IsOccupied(occupied, next.x, next.y)) continue;
                var a = RoomIndex(rooms, cell);
                var b = RoomIndex(rooms, next);
                var edge = EdgeKey(cell, next);
                if (a != b && !doorEdges.Contains(edge)) blocked.Add(edge);
            }
        }
        return blocked;
    }

    private static void CarveRoom(RectInt room, bool[,] occupied)
    {
        for (var x = room.xMin; x < room.xMax; x++)
        {
            for (var y = room.yMin; y < room.yMax; y++)
            {
                occupied[x, y] = true;
            }
        }
    }

    private static void CarveCell(bool[,] occupied, int x, int y)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            occupied[x, y] = true;
        }
    }

    private static void BuildFloor(Transform parent, bool[,] occupied, Material material)
    {
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                if (!occupied[x, y]) continue;
                CreateCube(
                    $"Floor_{x}_{y}",
                    parent,
                    GridToWorld(new Vector2Int(x, y), -0.1f),
                    new Vector3(TileSize, 0.2f, TileSize),
                    material);
            }
        }
    }

    private static List<DoorPlacement> KeepConnectedDoors(bool[,] occupied, List<DoorPlacement> candidates)
    {
        var connected = new List<DoorPlacement>();
        var existingEdges = new HashSet<DoorEdge>();

        foreach (var candidate in candidates)
        {
            if (!IsConnectedDoor(occupied, candidate))
            {
                // A door must sit between two carved cells. This prevents a
                // bad route or boundary case from placing a door into empty
                // space or through the outside wall of the dungeon.
                continue;
            }

            var edge = new DoorEdge(candidate.Cell, candidate.Direction);
            var reverseEdge = new DoorEdge(candidate.Cell + candidate.Direction, -candidate.Direction);
            if (existingEdges.Contains(edge) || existingEdges.Contains(reverseEdge))
            {
                continue;
            }

            existingEdges.Add(edge);
            connected.Add(candidate);
        }

        return connected;
    }

    private static bool IsConnectedDoor(bool[,] occupied, DoorPlacement door)
    {
        var neighbor = door.Cell + door.Direction;
        return IsOccupied(occupied, door.Cell.x, door.Cell.y) &&
               IsOccupied(occupied, neighbor.x, neighbor.y);
    }

    private static void BuildWalls(
        Transform parent,
        bool[,] occupied,
        List<DoorPlacement> doors,
        Material material,
        HashSet<long> blockedEdges = null)
    {
        var doorEdges = new HashSet<DoorEdge>();
        foreach (var door in doors)
        {
            doorEdges.Add(new DoorEdge(door.Cell, door.Direction));
            doorEdges.Add(new DoorEdge(door.Cell + door.Direction, -door.Direction));
        }

        var directions = new[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        var wallIndex = 0;
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                if (!occupied[x, y]) continue;

                var cell = new Vector2Int(x, y);
                foreach (var direction in directions)
                {
                    var neighbor = cell + direction;
                    var neighborOccupied = IsOccupied(occupied, neighbor.x, neighbor.y);
                    if (neighborOccupied && (blockedEdges == null || !blockedEdges.Contains(EdgeKey(cell, neighbor)))) continue;
                    if (neighborOccupied && (direction.x < 0 || direction.y < 0)) continue;
                    if (doorEdges.Contains(new DoorEdge(cell, direction))) continue;

                    var center = GridToWorld(cell, WallHeight * 0.5f) + new Vector3(direction.x, 0f, direction.y) * (TileSize * 0.5f);
                    var scale = direction.x != 0
                        ? new Vector3(WallThickness, WallHeight, TileSize)
                        : new Vector3(TileSize, WallHeight, WallThickness);
                    CreateCube($"Wall_{wallIndex++}", parent, center, scale, material);
                }
            }
        }
    }

    private static void BuildDoors(Transform parent, List<DoorPlacement> doors, Material material, int seed)
    {
        for (var i = 0; i < doors.Count; i++)
        {
            var door = doors[i];
            var center = GridToWorld(door.Cell, 0f) + new Vector3(door.Direction.x, 0f, door.Direction.y) * (TileSize * 0.5f);
            var horizontalOpening = door.Direction.y != 0;
            var doorRoot = new GameObject($"Door_{i + 1:00}");
            doorRoot.transform.SetParent(parent, false);
            doorRoot.transform.position = center;

            var panel = CreateCube(
                "Door_Panel",
                doorRoot.transform,
                new Vector3(0f, DoorHeight * 0.45f, 0f),
                horizontalOpening
                    ? new Vector3(DoorWidth * 0.86f, DoorHeight * 0.9f, WallThickness * 1.5f)
                    : new Vector3(WallThickness * 1.5f, DoorHeight * 0.9f, DoorWidth * 0.86f),
                material);
            var panelCollider = panel.GetComponent<Collider>();

            if (horizontalOpening)
            {
                CreateCube("Frame_Left", doorRoot.transform, new Vector3(-DoorWidth * 0.5f, DoorHeight * 0.5f, 0f), new Vector3(DoorFrameThickness, DoorHeight, WallThickness), material);
                CreateCube("Frame_Right", doorRoot.transform, new Vector3(DoorWidth * 0.5f, DoorHeight * 0.5f, 0f), new Vector3(DoorFrameThickness, DoorHeight, WallThickness), material);
                CreateCube("Frame_Header", doorRoot.transform, new Vector3(0f, DoorHeight, 0f), new Vector3(DoorWidth, DoorFrameThickness, WallThickness), material);
            }
            else
            {
                CreateCube("Frame_Front", doorRoot.transform, new Vector3(0f, DoorHeight * 0.5f, -DoorWidth * 0.5f), new Vector3(WallThickness, DoorHeight, DoorFrameThickness), material);
                CreateCube("Frame_Back", doorRoot.transform, new Vector3(0f, DoorHeight * 0.5f, DoorWidth * 0.5f), new Vector3(WallThickness, DoorHeight, DoorFrameThickness), material);
                CreateCube("Frame_Header", doorRoot.transform, new Vector3(0f, DoorHeight, 0f), new Vector3(WallThickness, DoorFrameThickness, DoorWidth), material);
            }

            var trigger = doorRoot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, DoorHeight * 0.45f, 0f);
            trigger.size = horizontalOpening
                ? new Vector3(TileSize, DoorHeight, TileSize * 0.75f)
                : new Vector3(TileSize * 0.75f, DoorHeight, TileSize);

            var behavior = doorRoot.AddComponent<PrototypeDoor>();
            behavior.Initialize(
                panel.transform,
                panelCollider,
                WorldRoomBounds(door.OwnerBounds),
                seed + i,
                door.Cell,
                door.Direction);
        }
    }

    private static Rect WorldRoomBounds(RectInt room)
    {
        var minimum = GridToWorld(new Vector2Int(room.xMin, room.yMin), 0f);
        var maximum = GridToWorld(new Vector2Int(room.xMax - 1, room.yMax - 1), 0f);
        return Rect.MinMaxRect(
            minimum.x - TileSize * 0.5f,
            minimum.z - TileSize * 0.5f,
            maximum.x + TileSize * 0.5f,
            maximum.z + TileSize * 0.5f);
    }

    private static void BuildTraps(Transform root, bool[,] occupied, int seed)
    {
        var trapRoot = CreateChild(root, "Level_Traps");
        var trapCells = new List<Vector2Int>();
        var candidates = new List<Vector2Int>();

        for (var x = 1; x < Width - 1; x++)
        {
            for (var y = 1; y < Height - 1; y++)
            {
                if (occupied[x, y]) candidates.Add(new Vector2Int(x, y));
            }
        }

        var random = new System.Random(seed + 1);
        while (trapCells.Count < Mathf.Min(8, candidates.Count) && candidates.Count > 0)
        {
            var index = random.Next(candidates.Count);
            var cell = candidates[index];
            candidates.RemoveAt(index);

            var tooClose = false;
            foreach (var existing in trapCells)
            {
                if (Vector2Int.Distance(existing, cell) < 2.5f)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose) trapCells.Add(cell);
        }

        for (var i = 0; i < trapCells.Count; i++)
        {
            var trapType = (PrototypeTrapType)random.Next(8);
            var color = TrapColor(trapType);
            var hidden = random.NextDouble() < 0.5;
            var trapObject = new GameObject($"Trap_{trapType}");
            trapObject.transform.SetParent(trapRoot, false);
            trapObject.transform.position = GridToWorld(trapCells[i], 0f);
            var material = CreateMaterial($"Level {trapType} Trap Material", color);

            var visualRoot = new GameObject("Trap_Visual").transform;
            visualRoot.SetParent(trapObject.transform, false);
            // Traps intentionally remain one world block wide inside the
            // enlarged four-unit terrain tile.
            DisableCollider(CreateCube("Marker_Base", visualRoot, new Vector3(0f, 0.08f, 0f), new Vector3(1f, 0.12f, 1f), material));
            DisableCollider(CreateCube("Marker_Core", visualRoot, new Vector3(0f, 0.28f, 0f), new Vector3(0.22f, 0.32f, 0.22f), material));

            var trigger = trapObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1f, 0f);
            trigger.size = new Vector3(1.2f, 2f, 1.2f);

            var trap = trapObject.AddComponent<PrototypeTrap>();
            trap.Initialize(trapType, hidden, color, visualRoot);
        }
    }

    private static void BuildItemsAndChests(Transform root, List<Room> rooms, int seed)
    {
        var itemRoot = CreateChild(root, "Level_Items");
        var random = new System.Random(seed + 321);
        var availableWeapons = new List<int>();
        for (var i = 0; i < PrototypeMeleeWeapons.Count; i++) availableWeapons.Add(i);
        SpawnWeapon(itemRoot, OffsetRoomCell(rooms[0], 1, 0), random, availableWeapons);
        if (rooms.Count >= 3) SpawnWeapon(itemRoot, OffsetRoomCell(rooms[2], -1, 0), random, availableWeapons);
        if (rooms.Count >= 5) SpawnWeapon(itemRoot, OffsetRoomCell(rooms[4], 1, -1), random, availableWeapons);

        if (rooms.Count < 2) return;

        var keyCell = OffsetRoomCell(rooms[1], -1, -1);
        PrototypeItemPickup.CreateRuntime(
            itemRoot,
            GridToWorld(keyCell, 0f),
            "Item_Key",
            "key",
            "Key",
            PrototypeEquipmentSlot.None,
            new Color(1f, 0.82f, 0.1f));

        if (rooms.Count >= 3)
        {
            var chestCell = OffsetRoomCell(rooms[2], 1, 1);
            var chestObject = new GameObject("Chest_Locked_01");
            chestObject.transform.SetParent(itemRoot, false);
            chestObject.transform.position = GridToWorld(chestCell, 0f);
            chestObject.transform.rotation = Quaternion.LookRotation(Vector3.forward);

            var chestMaterial = CreateMaterial("Locked Chest Material", new Color(0.42f, 0.2f, 0.08f));
            var chestVisual = CreateCube(
                "Chest_Visual",
                chestObject.transform,
                new Vector3(0f, 0.55f, 0f),
                new Vector3(1.8f, 1.1f, 1.3f),
                chestMaterial);
            DisableCollider(chestVisual);

            var chest = chestObject.AddComponent<PrototypeChest>();
            chest.Initialize(
                chestVisual.GetComponent<Renderer>(),
                "health_potion",
                "Health Potion",
                PrototypeEquipmentSlot.None,
                new Color(0.9f, 0.15f, 0.2f));
        }

        if (rooms.Count >= 4)
        {
            var potionCell = OffsetRoomCell(rooms[3], -1, 1);
            PrototypeItemPickup.CreateRuntime(
                itemRoot,
                GridToWorld(potionCell, 0f),
                "Item_HealthPotion",
                "health_potion",
                "Health Potion",
                PrototypeEquipmentSlot.None,
                new Color(0.9f, 0.15f, 0.2f));

            var foodCell = OffsetRoomCell(rooms[3], 1, -1);
            PrototypeItemPickup.CreateRuntime(
                itemRoot,
                GridToWorld(foodCell, 0f),
                "Item_Food",
                "food",
                "Food Ration",
                PrototypeEquipmentSlot.None,
                new Color(0.72f, 0.48f, 0.2f));
        }

    }

    private static void SpawnWeapon(Transform parent, Vector2Int cell, System.Random random, List<int> available)
    {
        var choice = random.Next(available.Count);
        var weapon = PrototypeMeleeWeapons.Get(available[choice]);
        available.RemoveAt(choice);
        PrototypeItemPickup.CreateRuntime(parent, GridToWorld(cell, 0f), $"Item_{weapon.Id}",
            weapon.Id, weapon.Name, PrototypeEquipmentSlot.Weapon, weapon.Color);
    }

    private static Vector2Int OffsetRoomCell(Room room, int xOffset, int yOffset)
    {
        return new Vector2Int(
            Mathf.Clamp(room.Center.x + xOffset, room.Bounds.xMin, room.Bounds.xMax - 1),
            Mathf.Clamp(room.Center.y + yOffset, room.Bounds.yMin, room.Bounds.yMax - 1));
    }

    private static void BuildExitMarker(Transform root, Vector2Int cell)
    {
        var exitRoot = new GameObject("Level_Exit");
        exitRoot.transform.SetParent(root, false);
        exitRoot.transform.position = GridToWorld(cell, 0f);

        var exitMaterial = CreateMaterial("Exit Marker Material", new Color(0.2f, 0.95f, 0.35f));
        var marker = CreateCube(
            "Exit_Marker",
            exitRoot.transform,
            new Vector3(0f, 0.18f, 0f),
            new Vector3(1.6f, 0.35f, 1.6f),
            exitMaterial);
        DisableCollider(marker);
    }

    private static bool ValidateConnectivity(bool[,] occupied, Vector2Int start, Vector2Int target, HashSet<long> blockedEdges)
    {
        if (!IsOccupied(occupied, start.x, start.y) || !IsOccupied(occupied, target.x, target.y))
        {
            return false;
        }

        var visited = new HashSet<Vector2Int> { start };
        var frontier = new Queue<Vector2Int>();
        frontier.Enqueue(start);
        var directions = new[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current == target)
            {
                return true;
            }

            foreach (var direction in directions)
            {
                var next = current + direction;
                if (IsOccupied(occupied, next.x, next.y) && !blockedEdges.Contains(EdgeKey(current, next)) && visited.Add(next))
                {
                    frontier.Enqueue(next);
                }
            }
        }

        return false;
    }

    private static Color TrapColor(PrototypeTrapType trapType)
    {
        return trapType switch
        {
            PrototypeTrapType.ToxicGas => new Color(0.1f, 1f, 0.1f),
            PrototypeTrapType.Fire => new Color(1f, 0.45f, 0.05f),
            PrototypeTrapType.ParalyticGas => new Color(1f, 0.9f, 0.05f),
            PrototypeTrapType.PoisonDart => new Color(0.65f, 0.1f, 0.85f),
            PrototypeTrapType.Alarm => new Color(0.9f, 0.05f, 0.05f),
            PrototypeTrapType.Lightning => new Color(0.1f, 0.4f, 1f),
            PrototypeTrapType.Gripping => new Color(0.5f, 0.52f, 0.55f),
            PrototypeTrapType.Summoning => new Color(0.35f, 0.8f, 1f),
            _ => Color.white
        };
    }

    private static void DisableCollider(GameObject visual)
    {
        var collider = visual.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    private static bool IsOccupied(bool[,] occupied, int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height && occupied[x, y];
    }

    public static Vector3 GridToWorld(Vector2Int cell, float y)
    {
        return new Vector3(
            (cell.x - (Width - 1) * 0.5f) * TileSize,
            y,
            (cell.y - (Height - 1) * 0.5f) * TileSize);
    }

    public static Vector2Int WorldToGrid(Vector3 world)
    {
        return new Vector2Int(
            Mathf.RoundToInt(world.x / TileSize + (Width - 1) * 0.5f),
            Mathf.RoundToInt(world.z / TileSize + (Height - 1) * 0.5f));
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static GameObject CreateCube(string objectName, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        return cube;
    }

    private static Material CreateMaterial(string materialName, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader)
        {
            name = materialName,
            color = color
        };
        material.SetFloat("_Smoothness", 0f);
        return material;
    }
}
