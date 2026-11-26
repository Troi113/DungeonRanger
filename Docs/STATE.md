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
- Floors 1–4 spawn rats only (stats in `PrototypeMobs`, scaled down from Pixel Dungeon). Rat: 4 health, 1–2 damage, brown. Albino Rat: 8 health, 1–2 damage, white, and a 50% chance per damaging bite to cause Bleeding (1 damage/second for 3 seconds). Each rat has a seeded `albinoRatChance` (default 1/30, as in Pixel Dungeon) to be albino. All mobs have 1 defense: each hit on them is reduced by 0–1, never below 1.
- Rats use 3D models from the Rat Pack (`Assets/ThirdParty/Resources/RatPack/`): Rat uses the brown model (`Rat3`), Albino Rat the white one (`Rat1`). `PrototypeMobModels` loads the FBX, applies the palette texture, scales each model to 2.8x its hit-box length (about 4 world units for a Rat), and turns it to face the direction of travel. Each rat keeps an invisible box collider (same size as before) for hits; sleeping rats are drawn darker. The pack has no animation or skeleton, so `PrototypeMobWalkMotion` fakes a walk: while a rat moves it bobs and rocks side to side at a rate tied to its speed, and settles when it stops. Goo is still a green sphere.
- Enemy contact damage rolls a bell-curved range like Pixel Dungeon's `NormalIntRange`. Goo keeps its 18 health, size, and charge (fixed 2 damage), and its normal attacks now roll 1–3 damage with a 33% chance to cause Ooze (1 damage/second for 5 seconds). Bleeding and Ooze bypass armor.
- Floor 5 generates a rectangular arena with a placeholder Goo boss. Defeating Goo drops the Amulet of Glory at its position. Press Space nearby to collect it, even with a full inventory, and enter the dedicated runtime `Victory` scene. The congratulations screen offers a return to the main menu.
- The same player object carries health, inventory, hunger and status effects between floors. Press R after death to restart the run.
- Sleeping enemies wake individually at random intervals of 20–35 seconds and patrol; this does not reveal the player's position. A sleeping enemy also wakes immediately if it sees the player within eight logical cells with a clear line of sight or is hit. Enemies that notice the player chase, then search the last seen position if sight is lost.
- Goo has a charge attack: after a 10-second cooldown it spends 1 second aiming, then rushes up to 18 world units at 4.5 times its normal chase speed in a fixed direction. The orange marker shows the aim during windup. The charge stops at walls; a ranger in its path takes damage once and is knocked back 6 world units (forward and out of the path) over 0.25 seconds, blocked by walls. Its cooldown starts after the charge ends. Goo is immune to hit slowing.
- The Goo drop, Space pickup with a full inventory, victory screen, and return to menu were checked in Play mode using a test helper to reach the boss. A complete run through all floors still needs playtesting. The victory scene is created at runtime rather than saved as a scene asset.

## Runtime setup
- `PrototypeBootstrap` generates the room, player, camera, and lighting at runtime.
- `PrototypeTerrainGenerator`: procedural 32x32 room with four doorways, floor, and walls.
- `PrototypeLevelGenerator`: seeded 32x32 floor built from six rectangular rooms connected by a nearby-room network plus an alternate route that forms a loop. Corridor routes go around unrelated rooms. Touching rooms share one door; separate rooms have a door at each end of their corridor. Remaining room boundaries have walls; enemy pathfinding and fog use the same blocked-edge data. Invalid placements are retried before terrain objects are created, with a fixed connected layout as a fallback.
  - Logical terrain tile is 4 Unity world units wide.
  - Each trap is one flat plate filling the 4x4 world-unit tile (four times the original width). Its trigger covers the whole tile and is 4 units tall.

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
- Searching follows Pixel Dungeon's `Hero.search()`, adapted to real time (`PrototypePlayerSearch`). Deliberate search: hold Space for 1.5 s, standing still, with nothing to interact with (walking restarts the timer); it always reveals every hidden, unsprung trap in the square of 6 tiles around the ranger. Passive noticing: on entering each new cell, every hidden trap in the 8 surrounding cells has a 10% (`awareness`) chance to be revealed. Both only reach cells currently visible through fog. The HUD shows a Searching bar, then "You noticed something." or "You search but find nothing."
- Hidden trap visuals reveal when the trap is activated. Visible, armed traps show their bright type color with a glow; once activated (by the ranger or a thrown item) the plate turns a dark, unlit shade of that color (`spentShade` 0.3, `armedGlow` 0.6 on `PrototypeTrap`).
- Enemies are stationary and wake visually when hit by the player.

## Player systems

- `PrototypePlayerStats` starts the player at 20 health and supports damage, healing, and death state.
- `PrototypeInventory` provides 20 slots. The first four are reserved for weapon, armor, artifact/ring A, and artifact/ring B; the remaining 16 are general inventory slots.
- `PrototypeStatusEffects` currently supports poisoned, toxic, paralyzed, and gripped states. Damage-over-time effects tick once per second; paralyzed and gripped block movement.
- `PrototypeHunger` starts satiated, counts moving time only, becomes Hungry after 60 moving seconds, becomes Famished after 120 moving seconds, and deals 1 damage every 5 seconds while Famished. These values are serialized prototype defaults and can be tuned later.
- `PrototypeGameUI` displays health and hunger and opens a 4-column by 5-row inventory with `I`. The top row is reserved for weapon, armor, artifact/ring A, and artifact/ring B.
- The ranger starts with a permanent bow in a general inventory slot; it requires no equipment slot and always fires with `J` during gameplay. Arrows travel at 24 world units per second for up to 40 world units and disappear on hitting enemies, walls, or closed doors; open doors allow arrows through. Normal enemies hit by `J` or `K` move at half speed for 1.5 seconds; repeated hits refresh the effect.
- `K` punches for 1 damage without a weapon. Each attack hits every enemy in a one-tile-wide box in front of the player: one tile (4 world units) deep for punches and all weapons except the spear, two tiles deep for the spear. The spear has the slowest attack (0.8s). Hits cannot pass through walls or closed doors.
- During gameplay, five consecutive presses of `Q` toggle the one-hit bow cheat. The HUD displays when it is active; other key presses interrupt an unfinished sequence. It carries between floors during a run and resets when a new player is created.
- Generated melee pickups include knuckledusters, knives, swords, sickles, axes, and spears, with prototype damage and cooldown values. A weapon appears near the starting room. Pixel Dungeon's melee weapons informed the variety and real-time feel; knife and sickle are prototype additions.
- Each generated non-boss floor places three scroll pickups, including one in the starting room. The six available types are Challenge, Upgrade, Remove Curse, Teleportation, Identify, and Magic Mapping. Matching scrolls stack in a single backpack slot. Their randomly assigned rune labels and known types persist across floors until the run is restarted.
- Every item except the permanent bow and worn cursed equipment can be dropped or thrown from its inventory action menu. Drop places the whole stack on the ranger's cell. Throw closes the inventory and throws one item straight in the facing direction: on a wall it lands in the cell before the wall; on a closed door it opens the door, lands under it, and keeps the door open while it lies there; on an untriggered (including hidden) trap it sets the trap off and lands on it. Traps are found by the grid cells the item passes over, not by physics, so fast throws cannot skip them. Thrown items do not hit enemies. Trap effects that need the ranger standing on the trap do nothing when an item triggers them; alarm and summoning still wake enemies. Landed items are picked up with Space.
- Scrolls are read from the paused inventory with `J`. Challenge alerts enemies on the current floor; Teleportation moves the ranger to a safe cell on that floor; Magic Mapping reveals the floor layout and hidden traps without revealing enemy positions; Remove Curse cleanses carried and equipped equipment. Upgrade and Identify ask the player to choose an eligible item with WASD/arrows and `J`; `Esc` cancels without consuming the scroll. Reading is instantaneous in this prototype.
- Each generated non-boss floor places one armor pickup in the second room: Cloth (tier 1, most common), Leather (tier 2), or Mail (tier 3, rarest). Equipped armor absorbs a random 0 to (tier + upgrade level) damage from each enemy or Goo hit; traps, poison, and hunger bypass armor. Values live in `PrototypeArmors`. Class armor, glyphs, and Strength requirements are not implemented.
- Equipping an unidentified weapon or armor has a `PrototypeInventory.equipCurseChance` (default 15%) chance to curse it, on top of the 1-in-5 generation curse. Identified items are safe to equip.
- Weapons may be unidentified or cursed. Cursed equipment cannot be removed or replaced until cleansed or upgraded. An Upgrade scroll targets melee weapons (+1 damage) or armor (+1 maximum absorption) and removes a curse. Rings, wands, and other upgrade effects are not implemented.
- In inventory, use WASD or arrow keys to select a slot, `J` to open or confirm an item action, and `Esc` to close its action menu. Potions heal 8 health, food restores 60 moving-seconds of hunger, and equipment can be equipped or unequipped. Both artifact/ring slots are selectable. Artifact/ring stat effects remain unimplemented.
- Enemies now attack the player at close range with a cooldown. Death displays a restart prompt; `R` reloads the active scene.
- Enemies, including Goo, show a health bar above their visual after taking damage. The bar tracks remaining health, stays hidden for undamaged enemies, and follows the enemy's fog-of-war visibility.
- Opening the inventory pauses game time; closing it with `I` or `Esc` resumes play (with an item action menu or scroll targeting open, `Esc` backs out of that first). With the inventory closed, Escape opens a pause menu with Continue and Quit to Main Menu; Continue resumes the run and quitting saves it as a game in progress. Exit Game closes a standalone build (Unity ignores application quit in the editor).
- The build scene list starts with MainMenu, followed by DungeonGameplay; opening MainMenu in the editor and pressing Play starts at the title screen.

## Menus and saving

- Main menu: Start, Leaderboards, Exit Game. Leaderboards is an empty placeholder screen with Back.
- Start opens Games in Progress. With no saved run it shows New Game and Back. With a saved run it also shows a Continue button with the floor and the last time played; New Game then asks before erasing the saved run.
- One save slot: `PrototypeSaveSystem` writes `game_in_progress.json` to `Application.persistentDataPath`. Quit to Main Menu and closing the game save the run; dying or collecting the amulet deletes it.
- Floors are regenerated from the run seed on load, then their saved changes are applied: picked-up generated items, dropped/thrown/chest items, enemy health/position/awake state and deaths, triggered and revealed traps, opened chest, explored fog, and a dropped amulet. Player health, hunger, inventory, known scrolls, floor, and position are restored. Status effects, door positions, the bow cheat, and enemy wake timers are not saved.
- Death is permanent: dying deletes the saved run immediately, and any later save attempt for a dead run deletes instead of writing.

## UI art

- All UI (HUD, prompt, inventory, item actions including Drop/Throw, pause, death, main menu, Games in Progress, new-game confirmation, Leaderboards, victory) is immediate-mode GUI drawn through `PrototypeUITheme` with the Free Medieval Fantasy UI Pack by Kibyra (`Assets/ThirdParty/Resources/FreeMedievalFantasyUIPack/`, see `Docs/CREDITS.md`).
- `Assets/Scripts/Editor/PixelArtTextureImporter.cs` imports those PNGs as readable, point-filtered, uncompressed sprites.
- The project is in Linear color space, where IMGUI washes colors out; `PrototypeUITheme` converts its colors and makes a one-time linearized copy of each texture. `BeginScaledGui` scales the UI to an 800px reference height (0.5x-2x).

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
| `K` | Punch or attack with an equipped melee weapon in the facing direction |
| `I` | Open/close the 4x5 inventory grid |
| `WASD` / arrows, `J`, `Esc` | Navigate inventory, select/confirm an action, and deselect |
| `Esc` | Close an item action menu or cancel scroll targeting; otherwise close the inventory and resume. With the inventory closed, open/close the pause menu |
| `Space` | Pick up a nearby item, open a chest while facing it with a key, or travel using stairs |
| Hold `Space` | Standing still with nothing to interact with, hold 1.5 s to search: reveals visible hidden traps within 6 tiles |
| `L` | Reserved for a future inventory/quick-slot action. Do not implement until defined. |

Auto-targeting is intentionally deferred. Bow shots travel straight.
