# Current Project State

Update this file at the end of each milestone.

## Project
- Unity project: `DungeonRanger` (Unity 6, URP)
- Scripts: `Assets/Scripts/`

## Scenes
- `Assets/Scenes/MainMenu.unity`: startup scene with Start Game and Exit Game.
- `Assets/Scenes/SampleScene.unity`: main test scene
- `Assets/Scenes/TrapsSleep.unity`: saved prototype scene (when present)
- `Assets/Scenes/LevelTest.unity`: full-floor generator test (when present)
- `Assets/Scenes/DungeonGameplay.unity`: playable five-floor prototype, generated at runtime.

## DungeonGameplay prototype

- Floors 1–4 use different per-run seeds and generated rooms, corridors, doors, traps and loot. Press Space near blue down stairs to descend or yellow up stairs to return to the previous floor; floor 1 has no previous floor. Each staircase occupies a 2×2 world-unit footprint.
- Explored floors stay in memory during a run, preserving fog discovery and the state of enemies, traps, doors, chests and pickups. Their enemy wake timers pause while the player is on another floor. Restarting clears the run.
- Floor 5 generates a rectangular arena with a placeholder Goo boss. Defeating Goo drops the Amulet of Glory at its position. Press Space nearby to collect it, even with a full inventory, and enter the dedicated runtime `Victory` scene. The congratulations screen offers a return to the main menu.
- The same player object carries health, inventory, hunger and status effects between floors. Press R after death to restart the run.
- Sleeping enemies wake individually at random intervals of 20–35 seconds and patrol; this does not reveal the player's position. A sleeping enemy also wakes immediately if it sees the player within eight logical cells with a clear line of sight or is hit. Enemies that notice the player chase, then search the last seen position if sight is lost.
- The Goo drop, Space pickup with a full inventory, victory screen, and return to menu were checked in Play mode using a test helper to reach the boss. A complete run through all floors still needs playtesting; Goo has no special attacks or phases yet. The victory scene is created at runtime rather than saved as a scene asset.

## Runtime setup
- `PrototypeBootstrap` generates the room, player, camera, and lighting at runtime.
- `PrototypeTerrainGenerator`: procedural 32x32 room with four doorways, floor, and walls.
- `PrototypeLevelGenerator`: seeded 32x32 floor built from six rectangular rooms connected by a nearby-room network plus an alternate route that forms a loop. Corridor routes go around unrelated rooms. Touching rooms share one door; separate rooms have a door at each end of their corridor. Remaining room boundaries have walls; enemy pathfinding and fog use the same blocked-edge data. Invalid placements are retried before terrain objects are created, with a fixed connected layout as a fallback.
  - Logical terrain tile is 4 Unity world units wide.
  - Trap markers are about 1x1 world unit.

## LevelTest prototype
- Seed `4582`, six attempted rooms, one-cell corridors.
- Visible doors, visible trap markers, and sleeping enemies.
- Sleeping enemies wake one at a time after randomized delays, or immediately when they see the player or are hit.
- Awake enemies patrol across generated walkable cells and detect the player within 8 logical cells (32 Unity world units) with clear line of sight.
- Fog of war gives the hero a circular 9-cell visibility radius (36 Unity world units).
- Closed doors block visibility; opening a door reveals the walkable area beyond it.
- Floor contents and enemies outside current visibility are hidden while walls and door frames remain visible as the map outline.
- Fog tracks separate visible, visited, and mapped states; visited and mapped cells use different dimmed fog colors.
- LevelTest now generates functional trap components. Each placed trap independently rolls one of eight types with equal 1/8 probability; each trap has a 50% chance to start hidden.
- Traps activate once when the player steps on them. Toxic, paralytic, poison, gripping, fire, and lightning traps apply prototype effects; alarm wakes all sleeping enemies; summoning currently wakes one sleeping enemy as a temporary stand-in for a future spawn system.
- LevelTest generates a key pickup, a locked chest, a health-potion pickup, and melee weapon pickups. Items are collected with Space; the chest consumes a key and drops a health potion.
- The generated last room has a green exit marker, and the generator validates that the entrance room and exit room are connected through carved floor cells.
- Geometry was checked across 120 seeds for reachable rooms and corridors, unique doors, and an alternate route. Complete movement, combat, and door playtesting across layouts remains open.

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
- The ranger starts with a permanent bow in a general inventory slot; it requires no equipment slot and always fires with `J` during gameplay. `K` uses the melee weapon equipped in the weapon slot, and does nothing without one.
- During gameplay, five consecutive presses of `Q` toggle the one-hit bow cheat. The HUD displays when it is active; other key presses interrupt an unfinished sequence. It carries between floors during a run and resets when a new player is created.
- Generated melee pickups include knuckledusters, knives, swords, sickles, axes, and spears, with prototype damage, reach, and cooldown values. A weapon appears near the starting room. Pixel Dungeon's melee weapons informed the variety and real-time feel; knife and sickle are prototype additions.
- In inventory, use WASD or arrow keys to select a slot, `J` to open or confirm an item action, and `Esc` to close its action menu. Potions heal 8 health, food restores 60 moving-seconds of hunger, and equipment can be equipped or unequipped. Both artifact/ring slots are selectable. Armor and artifact/ring stat effects remain unimplemented.
- Enemies now attack the player at close range with a cooldown. Death displays a restart prompt; `R` reloads the active scene.
- Opening the inventory pauses game time. Escape opens a pause menu with Continue and Quit to Main Menu; Continue resumes the run and quitting discards it. Exit Game closes a standalone build (Unity ignores application quit in the editor).
- The build scene list starts with MainMenu, followed by DungeonGameplay; opening MainMenu in the editor and pressing Play starts at the title screen.

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
| `Q` five times | Toggle the one-hit bow cheat during gameplay |
| `K` | Attack in the facing direction with an equipped melee weapon and a temporary slash effect |
| `I` | Open/close the 4x5 inventory grid |
| `WASD` / arrows, `J`, `Esc` | Navigate inventory, select/confirm an action, and deselect |
| `Esc` | Open/close the pause menu when no inventory item action is open |
| `Space` | Pick up a nearby item, open a chest while facing it with a key, or travel using stairs |
| `L` | Reserved for a future inventory/quick-slot action. Do not implement until defined. |

Auto-targeting is intentionally deferred. Bow shots travel straight.
