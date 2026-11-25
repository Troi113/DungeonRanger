# Current Project State

Update this file at the end of each milestone.

## Project
- Unity project: `DungeonRanger` (Unity 6, URP)
- Scripts: `Assets/Scripts/`

## Scenes
- `Assets/Scenes/SampleScene.unity`: main test scene
- `Assets/Scenes/TrapsSleep.unity`: saved prototype scene (when present)
- `Assets/Scenes/LevelTest.unity`: full-floor generator test (when present)
- `Assets/Scenes/DungeonGameplay.unity`: playable five-floor prototype, generated at runtime.

## DungeonGameplay prototype

- Floors 1–4 use different per-run seeds and generated rooms, corridors, doors, traps and loot. Press Space near blue down stairs to descend or yellow up stairs to return to the previous floor; floor 1 has no previous floor. Each staircase occupies a 2×2 world-unit footprint.
- Explored floors stay in memory during a run, preserving fog discovery and the state of enemies, traps, doors, chests and pickups. Their enemy wake timers pause while the player is on another floor. Restarting clears the run.
- Floor 5 generates a rectangular arena with a placeholder Goo boss. Goo chases and attacks using the prototype enemy behavior; defeating it shows a victory message.
- The same player object carries health, inventory, hunger and status effects between floors. Press R after death to restart the run.
- Sleeping enemies wake individually at random intervals of 20–35 seconds. Awake enemies patrol until they see the player within eight logical cells with a clear line of sight, or are hit. After losing sight they search the last seen position.
- This is an early gameplay prototype: floor-to-floor transitions and Goo combat still need full playtesting; Goo has no special attacks or phases yet.

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
- Awake enemies patrol across generated walkable cells and detect the player within 8 logical cells (32 Unity world units) with clear line of sight.
- Fog of war gives the hero a circular 9-cell visibility radius (36 Unity world units).
- Closed doors block visibility; opening a door reveals the walkable area beyond it.
- Floor contents and enemies outside current visibility are hidden while walls and door frames remain visible as the map outline.
- Fog tracks separate visible, visited, and mapped states; visited and mapped cells use different dimmed fog colors.
- LevelTest now generates functional trap components. Each placed trap independently rolls one of eight types with equal 1/8 probability; each trap has a 50% chance to start hidden.
- Traps activate once when the player steps on them. Toxic, paralytic, poison, gripping, fire, and lightning traps apply prototype effects; alarm wakes all sleeping enemies; summoning currently wakes one sleeping enemy as a temporary stand-in for a future spawn system.
- LevelTest now generates a key pickup, a locked chest, a health-potion pickup, and a ranger-bow pickup. Items are collected with Space; the chest consumes a key and drops a health potion.
- The generated last room has a green exit marker, and the generator validates that the entrance room and exit room are connected through carved floor cells.
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
- `PrototypeGameUI` displays health and hunger and opens a 4-column by 5-row inventory with `I`. The top row is reserved for weapon, armor, artifact/ring A, and artifact/ring B.
- Clicking a health potion heals 8 health, clicking food restores 60 moving-seconds of hunger, and clicking an equippable item in a general slot moves it into its equipment slot.
- Enemies now attack the player at close range with a cooldown. Death displays a restart prompt; `R` reloads the active scene.
- While the inventory is open, movement and combat are paused for the prototype.

## Pixel Dungeon reference decisions

- `RegularLevel` uses an entrance room, an exit room, graph connectivity, and regeneration when a layout is invalid. Our current prototype mirrors the entrance/exit and validation concepts; regeneration and scene transitions remain open work.
- `Level` keeps terrain, visited/mapped state, entrance/exit, mobs, item heaps, and environmental effects together. Our Unity version keeps these responsibilities split across generator, fog, enemy, item, and trap components so the live 3D prototype remains maintainable.
- `Room` and painter classes are the reference for future special-room generation. We currently have standard generated rooms only; treasure, trap, healing, and boss rooms remain TODO items.
- `Heap` is the reference for stackable item piles and chest contents. Our current items are individual pickups and the chest drops one reward; stacking and drop piles remain TODO items.
- `Hunger` is a timed status that advances during play and damages the hero while starving. Our live version advances only while moving, with Hungry and Famished states, to fit real-time movement.
- The original Java source is reference material only. New Unity C# systems remain original implementations.

## Controls

Current prototype movement speed is 8 Unity world units per second.

| Key | Action |
|---|---|
| `WASD` | Move and update facing direction |
| `Shift` | Tumble/dash about one enlarged terrain tile in the facing direction. Collision-aware, short cooldown. |
| `J` | Fire a straight bow projectile in the facing direction |
| `K` | Melee attack in the facing direction with a temporary slash effect |
| `I` | Open/close the 4x5 inventory grid |
| `Space` | Pick up a nearby item, open a chest while facing it with a key, or travel using stairs |
| `L` | Reserved for a future inventory/quick-slot action. Do not implement until defined. |

Auto-targeting is intentionally deferred. Bow shots travel straight.
