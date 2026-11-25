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
- Fog of war gives the hero a circular 9-cell visibility radius (36 Unity world units).
- Closed doors block visibility; opening a door reveals the walkable area beyond it.
- Floor contents and enemies outside current visibility are hidden while walls and door frames remain visible as the map outline.
- Fog tracks separate visible, visited, and mapped states; visited and mapped cells use different dimmed fog colors.
- LevelTest now generates functional trap components. Each placed trap independently rolls one of eight types with equal 1/8 probability; each trap has a 50% chance to start hidden.
- Traps activate once when the player steps on them. Toxic, paralytic, poison, gripping, fire, and lightning traps apply prototype effects; alarm wakes all sleeping enemies; summoning currently wakes one sleeping enemy as a temporary stand-in for a future spawn system.
- Not implemented yet: entrance/exit, layout validation, multiple-seed testing.

## Terrain test contents
- Two sleeping enemies.
- No stairs or cliffs.
- Eight possible trap types using the source-inspired names: Toxic Gas, Fire, Paralytic Gas, Poison Dart, Alarm, Lightning, Gripping, and Summoning.
- Trap colors: bright green, orange, yellow, purple, red, blue, grey, and light blue respectively.
- Hidden trap visuals reveal when the trap is activated; visible traps use their type color from spawn.
- Enemies are stationary and wake visually when hit by the player.

## Player systems

- `PrototypePlayerStats` starts the player at 20 health and supports damage, healing, and death state.
- `PrototypeInventory` provides 20 slots. The first four are reserved for weapon, armor, artifact/ring A, and artifact/ring B; the remaining 16 are general inventory slots.
- `PrototypeStatusEffects` currently supports poisoned, toxic, paralyzed, and gripped states. Damage-over-time effects tick once per second; paralyzed and gripped block movement.
- `PrototypeHunger` starts satiated, counts moving time only, becomes Hungry after 60 moving seconds, becomes Famished after 120 moving seconds, and deals 1 damage every 5 seconds while Famished. These values are serialized prototype defaults and can be tuned later.

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
