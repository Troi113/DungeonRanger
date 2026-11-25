# Current Project State

Update this file at the end of each milestone.

## Project
- Unity project: `DungeonRanger` (Unity 6, URP)
- Scripts: `Assets/Scripts/`

## Scenes
- `Assets/Scenes/SampleScene.unity`: main test scene
- `Assets/Scenes/TrapsSleep.unity`: saved prototype scene (when present)
- `Assets/Scenes/LevelTest.unity`: full-floor generator test (when present)

## Runtime setup
- `PrototypeBootstrap` generates the room, player, camera, and lighting at runtime.
- `PrototypeTerrainGenerator`: procedural 32x32 room with four doorways, floor, and walls.
- `PrototypeLevelGenerator`: seeded 32x32 floor built from several rectangular rooms and connecting corridors. Builds walls, visible door panels/triggers at room boundaries and corridor junctions, traps, player spawn data, and enemy spawn data.
  - Logical terrain tile is 4 Unity world units wide.
  - Trap markers are about 1x1 world unit.

## LevelTest prototype
- Seed `4582`, six attempted rooms, one-cell corridors.
- Visible doors, visible trap markers, and sleeping enemies.
- Sleeping enemies wake one at a time after randomized delays.
- Awake enemies patrol across generated walkable cells and chase the player only within 8 logical cells (32 Unity world units).
- Not implemented yet: entrance/exit, layout validation, multiple-seed testing.

## Terrain test contents
- Two sleeping enemies.
- No stairs or cliffs.
- One visible marker per original Pixel Dungeon trap type.
- Enemies are stationary and wake visually when hit by the player.

## Controls

Current prototype movement speed is 8 Unity world units per second.

| Key | Action |
|---|---|
| `WASD` | Move and update facing direction |
| `Shift` | Tumble/dash about one enlarged terrain tile in the facing direction. Collision-aware, short cooldown. |
| `J` | Fire a straight bow projectile in the facing direction |
| `K` | Melee attack in the facing direction with a temporary slash effect |
| `L` | Reserved for a future inventory/quick-slot action. Do not implement until defined. |

Auto-targeting is intentionally deferred. Bow shots travel straight.
