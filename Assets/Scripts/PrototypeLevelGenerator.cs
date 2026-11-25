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
    private const float TileSize = 4f;
    private const float WallThickness = 0.25f;
    private const float WallHeight = 2.5f;
    private const float DoorWidth = 3f;
    private const float DoorHeight = 2.2f;
    private const float DoorFrameThickness = 0.12f;
    private const int DesiredRooms = 6;
    private const int RoomPadding = 1;

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

    public sealed class Layout
    {
        public Vector3 PlayerSpawn;
        public readonly List<Vector3> EnemySpawns = new();
    }

    public Layout Build(Transform root)
    {
        var occupied = new bool[Width, Height];
        var rooms = CreateRooms(occupied);
        var doorPlacements = new List<DoorPlacement>();

        for (var i = 1; i < rooms.Count; i++)
        {
            ConnectRooms(rooms[i - 1], rooms[i], occupied, doorPlacements, i % 2 == 0);
        }

        // Guarantee a useful connected layout even if random placement produced
        // fewer rooms than expected.
        if (rooms.Count == 0)
        {
            var fallback = new Room(new RectInt(4, 4, 8, 8));
            rooms.Add(fallback);
            CarveRoom(fallback.Bounds, occupied);
        }

        var floorRoot = CreateChild(root, "Level_Floor");
        var wallRoot = CreateChild(root, "Level_Walls");
        var doorRoot = CreateChild(root, "Level_Doors");
        var floorMaterial = CreateMaterial("Level Floor Material", FloorColor);
        var wallMaterial = CreateMaterial("Level Wall Material", WallColor);
        var doorMaterial = CreateMaterial("Level Door Material", DoorColor);
        var connectedDoors = KeepConnectedDoors(occupied, doorPlacements);
        AddRoomBoundaryDoors(occupied, rooms, connectedDoors);
        AddCorridorJunctionDoors(occupied, rooms, connectedDoors);

        BuildFloor(floorRoot, occupied, floorMaterial);
        BuildWalls(wallRoot, occupied, connectedDoors, wallMaterial);
        BuildDoors(doorRoot, connectedDoors, doorMaterial);
        BuildTraps(root, occupied, rooms);

        var layout = new Layout
        {
            PlayerSpawn = GridToWorld(rooms[0].Center, 1f)
        };

        for (var i = 1; i < rooms.Count; i++)
        {
            layout.EnemySpawns.Add(GridToWorld(rooms[i].Center, 1f));
        }

        return layout;
    }

    private static List<Room> CreateRooms(bool[,] occupied)
    {
        var random = new System.Random(Seed);
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

    private static void ConnectRooms(
        Room from,
        Room to,
        bool[,] occupied,
        List<DoorPlacement> doors,
        bool horizontalFirst)
    {
        var start = from.Center;
        var end = to.Center;
        var delta = end - start;
        var useHorizontalFirst = (horizontalFirst && delta.x != 0) || delta.y == 0;
        Vector2Int startDoorDirection;
        Vector2Int endDoorDirection;
        Vector2Int startDoorCell;
        Vector2Int endDoorCell;

        if (useHorizontalFirst)
        {
            // The first corridor segment leaves the source room horizontally.
            // The final segment enters the destination room vertically.
            startDoorDirection = new Vector2Int(Math.Sign(delta.x), 0);
            endDoorDirection = delta.y != 0
                ? new Vector2Int(0, -Math.Sign(delta.y))
                : new Vector2Int(-Math.Sign(delta.x), 0);
            startDoorCell = MoveToRoomBoundary(from.Bounds, start, startDoorDirection);
            endDoorCell = MoveToRoomBoundary(to.Bounds, end, endDoorDirection);

            CarveHorizontal(start.x, end.x, start.y, occupied);
            CarveVertical(start.y, end.y, end.x, occupied);
        }
        else
        {
            // The first corridor segment leaves the source room vertically.
            // The final segment enters the destination room horizontally.
            startDoorDirection = new Vector2Int(0, Math.Sign(delta.y));
            endDoorDirection = delta.x != 0
                ? new Vector2Int(-Math.Sign(delta.x), 0)
                : new Vector2Int(0, -Math.Sign(delta.y));
            startDoorCell = MoveToRoomBoundary(from.Bounds, start, startDoorDirection);
            endDoorCell = MoveToRoomBoundary(to.Bounds, end, endDoorDirection);

            CarveVertical(start.y, end.y, start.x, occupied);
            CarveHorizontal(start.x, end.x, end.y, occupied);
        }

        // Every room-to-room corridor owns two doors: one at each end.
        doors.Add(new DoorPlacement(startDoorCell, startDoorDirection, from.Bounds));
        doors.Add(new DoorPlacement(endDoorCell, endDoorDirection, to.Bounds));
    }

    private static Vector2Int MoveToRoomBoundary(RectInt room, Vector2Int center, Vector2Int direction)
    {
        if (direction.x > 0) return new Vector2Int(room.xMax - 1, center.y);
        if (direction.x < 0) return new Vector2Int(room.xMin, center.y);
        if (direction.y > 0) return new Vector2Int(center.x, room.yMax - 1);
        return new Vector2Int(center.x, room.yMin);
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

    private static void CarveHorizontal(int fromX, int toX, int y, bool[,] occupied)
    {
        var min = Mathf.Min(fromX, toX);
        var max = Mathf.Max(fromX, toX);
        for (var x = min; x <= max; x++)
        {
            CarveCell(occupied, x, y);
        }
    }

    private static void CarveVertical(int fromY, int toY, int x, bool[,] occupied)
    {
        var min = Mathf.Min(fromY, toY);
        var max = Mathf.Max(fromY, toY);
        for (var y = min; y <= max; y++)
        {
            CarveCell(occupied, x, y);
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

    private static void AddRoomBoundaryDoors(
        bool[,] occupied,
        List<Room> rooms,
        List<DoorPlacement> doors)
    {
        var existingEdges = new HashSet<DoorEdge>();
        foreach (var door in doors)
        {
            existingEdges.Add(new DoorEdge(door.Cell, door.Direction));
        }

        var directions = new[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        foreach (var room in rooms)
        {
            for (var x = room.Bounds.xMin; x < room.Bounds.xMax; x++)
            {
                for (var y = room.Bounds.yMin; y < room.Bounds.yMax; y++)
                {
                    var cell = new Vector2Int(x, y);
                    foreach (var direction in directions)
                    {
                        var neighbor = cell + direction;
                        if (!IsOccupied(occupied, neighbor.x, neighbor.y) || room.Bounds.Contains(neighbor))
                        {
                            continue;
                        }

                        AddDoorIfNew(doors, existingEdges, cell, direction, room.Bounds);
                    }
                }
            }
        }
    }

    private static void AddCorridorJunctionDoors(
        bool[,] occupied,
        List<Room> rooms,
        List<DoorPlacement> doors)
    {
        var existingEdges = new HashSet<DoorEdge>();
        foreach (var door in doors)
        {
            existingEdges.Add(new DoorEdge(door.Cell, door.Direction));
        }

        var directions = new[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(0, 1)
        };

        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                var cell = new Vector2Int(x, y);
                if (!IsOccupied(occupied, x, y) || CountOccupiedNeighbors(occupied, cell) < 3)
                {
                    continue;
                }

                foreach (var direction in directions)
                {
                    var neighbor = cell + direction;
                    if (!IsOccupied(occupied, neighbor.x, neighbor.y) ||
                        AreInSameRoom(rooms, cell, neighbor) ||
                        CountOccupiedNeighbors(occupied, neighbor) < 2)
                    {
                        continue;
                    }

                    AddDoorIfNew(doors, existingEdges, cell, direction, FindNearestRoom(rooms, cell));
                }
            }
        }
    }

    private static void AddDoorIfNew(
        List<DoorPlacement> doors,
        HashSet<DoorEdge> existingEdges,
        Vector2Int cell,
        Vector2Int direction,
        RectInt ownerBounds)
    {
        var edge = new DoorEdge(cell, direction);
        var reverseEdge = new DoorEdge(cell + direction, -direction);
        if (existingEdges.Contains(edge) || existingEdges.Contains(reverseEdge))
        {
            return;
        }

        existingEdges.Add(edge);
        doors.Add(new DoorPlacement(cell, direction, ownerBounds));
    }

    private static int CountOccupiedNeighbors(bool[,] occupied, Vector2Int cell)
    {
        var count = 0;
        if (IsOccupied(occupied, cell.x + 1, cell.y)) count++;
        if (IsOccupied(occupied, cell.x - 1, cell.y)) count++;
        if (IsOccupied(occupied, cell.x, cell.y + 1)) count++;
        if (IsOccupied(occupied, cell.x, cell.y - 1)) count++;
        return count;
    }

    private static bool AreInSameRoom(List<Room> rooms, Vector2Int first, Vector2Int second)
    {
        foreach (var room in rooms)
        {
            if (room.Bounds.Contains(first) && room.Bounds.Contains(second))
            {
                return true;
            }
        }

        return false;
    }

    private static RectInt FindNearestRoom(List<Room> rooms, Vector2Int cell)
    {
        if (rooms.Count == 0)
        {
            return new RectInt(cell.x, cell.y, 1, 1);
        }

        var nearest = rooms[0];
        var nearestDistance = Vector2Int.Distance(cell, nearest.Center);
        foreach (var room in rooms)
        {
            var distance = Vector2Int.Distance(cell, room.Center);
            if (distance < nearestDistance)
            {
                nearest = room;
                nearestDistance = distance;
            }
        }

        return nearest.Bounds;
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
        Material material)
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
                    if (IsOccupied(occupied, neighbor.x, neighbor.y)) continue;
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

    private static void BuildDoors(Transform parent, List<DoorPlacement> doors, Material material)
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
            behavior.Initialize(panel.transform, panelCollider, WorldRoomBounds(door.OwnerBounds), Seed + i);
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

    private static void BuildTraps(Transform root, bool[,] occupied, List<Room> rooms)
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

        var random = new System.Random(Seed + 1);
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

        var definitions = new[]
        {
            ("Fire", new Color(1f, 0.45f, 0.05f)),
            ("Poison", new Color(0.65f, 0.1f, 0.85f)),
            ("Toxic", new Color(0.1f, 1f, 0.1f)),
            ("Paralytic", new Color(1f, 0.9f, 0.05f)),
            ("Alarm", new Color(0.9f, 0.05f, 0.05f)),
            ("Lightning", new Color(0.1f, 0.4f, 1f)),
            ("Gripping", new Color(0.5f, 0.52f, 0.55f)),
            ("Summoning", new Color(0.35f, 0.8f, 1f))
        };

        for (var i = 0; i < trapCells.Count; i++)
        {
            var trap = new GameObject($"Trap_{definitions[i].Item1}");
            trap.transform.SetParent(trapRoot, false);
            trap.transform.position = GridToWorld(trapCells[i], 0f);
            var material = CreateMaterial($"Level {definitions[i].Item1} Trap Material", definitions[i].Item2);
            // Traps intentionally remain one world block wide inside the
            // enlarged four-unit terrain tile.
            CreateCube("Marker_Base", trap.transform, new Vector3(0f, 0.08f, 0f), new Vector3(1f, 0.12f, 1f), material);
            CreateCube("Marker_Core", trap.transform, new Vector3(0f, 0.28f, 0f), new Vector3(0.22f, 0.32f, 0.22f), material);
        }
    }

    private static bool IsOccupied(bool[,] occupied, int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height && occupied[x, y];
    }

    private static Vector3 GridToWorld(Vector2Int cell, float y)
    {
        return new Vector3(
            (cell.x - (Width - 1) * 0.5f) * TileSize,
            y,
            (cell.y - (Height - 1) * 0.5f) * TileSize);
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
