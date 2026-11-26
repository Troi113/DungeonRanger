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

- Floors 1–4 use different per-run seeds and generated rooms, corridors, doors, traps and loot. Press Space near blue down stairs to descend or yellow up stairs to return to the previous floor; floor 1 has no previous floor. Each staircase occupies a 2×2 world-unit footprint. Stairs use the natural KayKit stone `stairs` model at real proportions (3.2 wide, about 3.3 tall), pointed at a walkable neighboring cell. Up stairs (yellow) rise from the stand point; down stairs (blue) sink into a dark pit cut into the neighboring cell, top step next to the stand point and descending away (the pit cell's invisible floor collider remains, so it cannot be fallen into), with a pulsing colored aura (`PrototypeGlowPulse` point light plus faint emission): blue for down, yellow for up. Floor 1 also has yellow up stairs at the spawn point; using them shows "You cannot leave the dungeon!". Without the pack, stairs fall back to box steps.
- Explored floors stay in memory during a run, preserving fog discovery and the state of enemies, traps, doors, chests and pickups. Their enemy wake timers pause while the player is on another floor. Restarting clears the run.
- Floors 1–4 spawn rats only (stats in `PrototypeMobs`, scaled down from Pixel Dungeon). Rat: 4 health, 1–2 damage, brown. Albino Rat: 8 health, 1–2 damage, white, and a 50% chance per damaging bite to cause Bleeding (1 damage/second for 3 seconds). Each rat has a seeded `albinoRatChance` (default 1/30, as in Pixel Dungeon) to be albino. All mobs have 1 defense: each hit on them is reduced by 0–1, never below 1.
- Rats use 3D models from the Rat Pack (`Assets/ThirdParty/Resources/RatPack/`): Rat uses the brown model (`Rat3`), Albino Rat the white one (`Rat1`). `PrototypeMobModels` loads the FBX, applies the palette texture, scales each model to 2.8x its hit-box length (about 4 world units for a Rat), and turns it to face the direction of travel. Each rat keeps an invisible box collider (same size as before) for hits; sleeping rats are drawn darker. The pack has no animation or skeleton, so `PrototypeMobWalkMotion` fakes a walk: while a rat moves it bobs and rocks side to side at a rate tied to its speed, and settles when it stops. Goo is an original cute slime built from primitives (`PrototypeGooFace`): a green body 9×6×9 (three times its old size) with a teardrop tip, droplet, big eyes, and a small mouth. When hit it squashes and pulls a random silly face for 0.8 s (squint > <, shocked O O, or derp with tongue out). It leaves slime puddles every 2.5 units it moves (`PrototypeSlimeTrail`, 2.6 radius, 9 s); a ranger in slime moves at 45% speed and cannot tumble.
- Enemy contact damage rolls a bell-curved range like Pixel Dungeon's `NormalIntRange`. Goo keeps its 18 health, size, and charge (fixed 2 damage), and its normal attacks now roll 1–3 damage with a 33% chance to cause Ooze (1 damage/second for 5 seconds). Bleeding and Ooze bypass armor.
- Floor 5 follows Pixel Dungeon's `SewerBossLevel`: a normal generated sewer layout (entrance room with yellow up stairs, tunnels, and Goo's room as the last room on the path), traps and items as usual, Goo as the only monster, and shallow sewer water pools on roughly 15% of the floor (`PrototypeSewerWater`, PD's Patch method with a 0.4 fill (`BossWaterFill`) and 5 smoothing passes; PD's 0.5 flooded about half the floor). Goo starts in its room and can squeeze through one-tile corridors to chase the ranger (its hit/movement body is a 1.6-radius sphere while the model stays big; the model squeezes thin in corridors). It heals 1 health every 1.5 s while standing in water (`PrototypeGooWaterHeal`). When the ranger enters Goo's room, Goo is alerted and the way back is sealed (up stairs refuse with "The way back is sealed!") until Goo dies. When Goo dies it swells, wobbles, and bursts into ~46 glossy goo blobs plus its two eyes that bounce, splat into puddles, and shrink away after a few seconds (`PrototypeGooExplosion`), with a big hit-stop and shake. Defeating Goo drops the Amulet of Glory; picking it up wins. (The old `BuildBossArena` method is kept but unused.)
- The same player object carries health, inventory, hunger and status effects between floors. Press R after death to restart the run.
- Sleeping enemies wake individually at random intervals of 20–35 seconds and patrol; this does not reveal the player's position. A sleeping enemy also wakes immediately if it sees the player within eight logical cells with a clear line of sight or is hit. Enemies that notice the player chase, then search the last seen position if sight is lost.
- Goo's attacks: contact hits (1–3, reach 4.2); a telegraphed charge every 10 s (1 s aim, up to 18 units at 4.5× speed) that deals 7–11 damage before armor with heavy shake and knockback; and a caustic glob every 5–9 s when it can see the ranger (0.8 s visible swelling, then a 13 u/s projectile stopped by walls) that slows the ranger to 50% and deals 1 damage per second for 3 s. Goo and the ranger never push each other's collision bodies, and a fall below the floor returns the ranger to the last safe spot.
- The Goo drop, Space pickup with a full inventory, victory screen, and return to menu were checked in Play mode using a test helper to reach the boss. A complete run through all floors still needs playtesting. The victory scene is created at runtime rather than saved as a scene asset.

## Runtime setup
- `PrototypeBootstrap` generates the room, player, camera, and lighting at runtime.
- `PrototypeTerrainGenerator`: procedural 32x32 room with four doorways, floor, and walls.
- `PrototypeLevelGenerator`: seeded 32x32 floor built from six rectangular rooms connected by a nearby-room network plus an alternate route that forms a loop. Corridor routes go around unrelated rooms. Touching rooms share one door; separate rooms have a door at each end of their corridor. Remaining room boundaries have walls; enemy pathfinding and fog use the same blocked-edge data. Invalid placements are retried before terrain objects are created, with a fixed connected layout as a fallback.
  - Logical terrain tile is 4 Unity world units wide.
  - Each trap uses the project's low-poly trap model (`PrototypeTrapModel`, rebuilt in C# from the three.js trap customizer): octagonal stone base, iron trim with corner spikes and claws, and rune strips/crystals in the trap type's color, scaled to about 0.9 of the tile. Its trigger covers the whole tile and is 4 units tall. Only the rune parts change with state (bright and glowing when armed, dark when spent).

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
- Hidden trap visuals reveal when the trap is activated. Visible, armed traps' runes show their bright type color with a glow; once activated (by the ranger or a thrown item) the runes turn a dark, unlit shade of that color (`spentShade` 0.3, `armedGlow` 0.6 on `PrototypeTrap`).
- Enemies are stationary and wake visually when hit by the player.

## Player systems

- `PrototypePlayerStats` starts the player at 20 health and supports damage, healing, and death state.
- `PrototypeInventory` provides 20 slots. The first four are reserved for weapon, armor, artifact/ring A, and artifact/ring B; the remaining 16 are general inventory slots.
- `PrototypeStatusEffects` currently supports poisoned, toxic, paralyzed, and gripped states. Damage-over-time effects tick once per second; paralyzed and gripped block movement.
- `PrototypeRegeneration` heals 1 health every 5 seconds of game time while the ranger is not Starving and below max health (after Pixel Dungeon's Regeneration). It stops while Hungry or Famished and while paused.
- Hunger: see Buffs and debuffs below (Pixel Dungeon Hunger rules).
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

## Items and Strength (after Pixel Dungeon)

- `PrototypeItemGenerator` plans each floor's items like PD's Level/RegularLevel/Generator: every regular floor (1–4) gets a food ration; quota items are 2 Potions of Strength and 3 Scrolls of Upgrade over floors 1–4 (the floors are chosen once from the run seed); then 3 random items plus one more per 40% roll, each picking a category by weight (potion 50, scroll 40, weapon 15, armor 10) with that weight halving afterwards. Random potions are Healing; random scrolls are Identify 30, Remove Curse 15, Teleportation 10, Magic Mapping 10, Challenge 8 (Upgrade and Strength only come from the quota). Random weapons and armor are unidentified with a 20% chance of +1/+2 and a 20% chance of a curse. The boss floor gets random items only.
- Items are placed on random room cells, avoiding traps and the stairs. Floor 1 also has a starter knife or knuckleduster in the entrance room. The locked chest holds one random item from the same generator.
- Strength: the ranger starts with 10 (shown as STR on the HUD and saved with the run). Potion of Strength (Drink) gives +1 permanently. Weapons need 10 (knuckleduster, knife), 11 (sickle), 12 (sword, spear), or 13 (axe); armor needs 7 + 2 × tier (cloth 9, leather 11, mail 13). Each upgrade lowers the need by 1. Too weak for a weapon: +25% attack delay per missing point. Extra Strength over a weapon's need: 0..excess bonus damage per hit. Too weak for armor: movement divided by 1.3 per missing point. Item info shows the requirement (`PrototypeStrength`).

## Buffs and debuffs (after Pixel Dungeon)

- `PrototypeStatusEffects` acts once per turn (1 s): Burning (8 turns, 1–4 damage per turn, may burn up a random unequipped scroll, can go out early, more likely at low health, and water douses it), Bleeding (level; each turn becomes random(level/2..level−1) damage), Poisoned (deals left/3 + 1 per turn, counting down), Toxic and Ooze (1 per turn), Crippled (half movement), Slowed (half movement and double attack delay), Paralysed (no moving or attacking), Rooted/Gripped (no moving), Invisible, Barkskin. Fire blobs reignite Burning; the albino rat's bite sets Bleeding to the damage dealt; poison darts poison for 4 + floor/2 turns.
- Hunger is a debuff (`PrototypeHunger`, PD Hunger): the level rises 1 per second of game time; Hungry at 260, Starving at 360; while starving, a 30% chance of 1 damage every 10 s. A ration satisfies 260, a berry 87. Regeneration only stops while Starving.
- The HUD shows a buff indicator under the health panel: one tag per active buff or debuff (including Hungry/Starving) with its time left (Bleeding shows its level).

## Blobs: gas and fire (after Pixel Dungeon)

- `PrototypeBlobs` (one per floor) holds per-cell amounts of toxic gas, paralytic gas, and fire, ticking every 0.5 s (one PD turn). Gas spreads like PD's `Blob.evolve`: each open cell becomes the average of itself and its open orthogonal neighbors, minus 1 (walls between cells block it). Toxic gas deals (max HP + 5 + 5 × floor) / 40 per tick (random rounding) and cancels paralytic gas where they meet; paralytic gas paralyzes the ranger (no moving or attacking) and enemies while they are in it; fire cells lose 1 per tick and set the ranger on fire (Burning, 1/s for 3 s) and deal 1 per tick to enemies. Goo takes half toxic damage. There are no flammable cells yet, so fire does not spread. Gas and fire are drawn with particles and a fire light. Blobs are not saved with the run.
- Gas and fire traps now release blobs on their cell (PD amounts: toxic 300 + 20 × floor, paralytic 80 + 5 × floor, fire on the 3×3 cells), also when set off by a thrown item. On an open room a floor-1 toxic cloud reaches about 25 cells and lasts about 5.5 s; paralytic about 13 cells for 3 s. Liquid Flame lights its 3×3 cells for 4 s.

## Potions (after Pixel Dungeon)

- `PrototypePotions` (on the player): five potions — Healing (`health_potion`: full heal, cures poison, toxic gas, bleeding), Strength (`strength_potion`: +1 STR), Experience (`potion_experience`: gain the next level), Invisibility (`potion_invisibility`: enemies, including Goo's glob, cannot see the ranger for 15 s; attacking with bow or melee ends it; the Ranger is drawn as a pale blue ghost), and Liquid Flame (`potion_liquid_flame`: fire blobs on the 3×3 cells where it breaks, for 4 s).
- Each run gives every potion a random color name ("crimson potion"); drinking identifies it, Liquid Flame is also identified when it shatters, and a Scroll of Identify works on potions. Potions stack. Icons and pickup colors follow the random color, never the type.
- Drink applies the potion to the ranger; Throw shatters it where it lands or on the first enemy it hits (Liquid Flame ignites; others splash harmlessly). Like PD, drinking a known harmful potion or throwing a known helpful one needs a second J press to confirm.
- Random potion drops (PD weights): Healing 45, Liquid Flame 15, Invisibility 10, Experience 4. Strength only comes from the quota (2 per run).

## Experience and levels

- Hero level starts at 1; each level needs 5 + 5 × level experience and gives +5 max health (and heals 5). Rats give 1 experience (none above level 5), Goo 10. The HUD shows Lv, STR, and a thin experience bar. Level, experience, max health, and known potions are saved with the run.

## Talents (Huntress tier 1, adapted from Shattered Pixel Dungeon)

- `PrototypeTalents`: 1 point per level at levels 2–5 (4 points), each talent up to rank 2. Press **T** to open the talent screen (pauses; W/S choose, J spend, T/Esc close). A level-up notice says when a point is ready. Ranks are saved with the run.
- Nature's Bounty: kills can drop a Berry (30%/50%, up to 2/4 per floor); eating a berry heals 2 and restores 25 hunger-seconds.
- Survivalist's Intuition: unidentified gear in the pack identifies itself after 60 s carried (rank 1) or right after pickup (rank 2).
- Followup Strike: after a bow hit, the next melee hit on that enemy within 4 s deals +1/+2 damage.
- Nature's Aid: eating or drinking grants Barkskin: +2 armor absorption for 5 s (rank 1) or +3 for 8 s (rank 2).

## Score, high scores, and settings

- Score: 10 per Rat, 30 per Albino Rat, 500 for Goo, 100 for each new floor reached, and on victory 1000 plus a time bonus of max(0, 900 − seconds played). The run clock counts unpaused play time. Score, time, and deepest floor are saved with the run.
- The HUD's right panel shows floor, score, and time.
- `PrototypeLeaderboard` keeps the top 10 runs (score, deepest floor or Victory, time, date) in `leaderboard.json` in `Application.persistentDataPath`; a run is recorded when the ranger dies or wins. Main menu → Leaderboards lists them.
- Ranger death (`PrototypeHeroDeath`): a shake, red sparks and a red screen flash, then slow motion (30% speed for 1.2 s real time) while the Ranger plays Death_A or Death_B and the camera pushes in; the game-over screen fades in after 1.8 s. Retry restores the camera.
- Game-over screen: score, floor reached, time, "New high score!" or the best score, with Retry (or R) and Main Menu buttons. The victory screen also shows score and time.
- `PrototypeSettings` saves master volume and fullscreen in PlayerPrefs and applies them at startup; main menu → Settings has a volume slider and a fullscreen toggle (fullscreen only applies in builds).

## Music

- `PrototypeMusic` (created automatically, kept across scenes) plays `menu_theme` on the main menu, `dungeon` on floors 1–4, `boss_goo` on floor 5 while Goo's fight is on, and `victory` once on the ending screen, crossfading between them. Volume follows Settings.
- The tracks are original, orchestral-style pieces synthesized by `Tools/generate_music.py` (strings, harp/pizzicato, cello, timpani, brass, reverb) into `Assets/Audio/Resources/Music/`. To swap a song, replace the file with one of the same name (WAV, OGG, or MP3), or edit and rerun the script.

## 3D art (KayKit, CC0)

- The ranger is the KayKit Adventurers Ranger (`PrototypeHeroModel`): the capsule renderer is hidden, the model is 2.3 units tall, turns to the facing direction, and plays `Idle_A`, `Running_A`, and `Death_A` from the Rig_Medium animation files through a Legacy Animation component.
- Melee attacks are animated on the Ranger in code (`PrototypeHeroAttackAnimation`; the free KayKit pack has no combat clips): the right arm bones are bent toward the strike with a small lunge. Bare hands and the knuckleduster punch; sword, knife, sickle, and axe alternate slash and stab; the spear alternates stab and slash with a longer lunge. The equipped weapon is shown in the right hand slot using KayKit models (sword_1handed, dagger for knife/sickle, axe_1handed, and a spear made from a stretched staff with a dagger tip; `Assets/ThirdParty/Resources/KayKit/Weapons/`). The old orange box swing only appears if the Ranger model is missing.
- Attack feel: each swing has a wind-up (arm cocked back, chest turned), a snappy strike with a forward lunge and a white swoosh trail from the fist or blade tip, and a follow-through. The melee hit now lands at the strike moment (about a third of the swing after pressing K), not on the key press. On a hit: a brief hit-stop (time slows to 5% for 0.06–0.09 s), camera shake, a spark burst, and the enemy flashes white and squashes (`PrototypeGameFeel`, `PrototypeHitFlash`, `PrototypeCameraFollow.AddShake`). Arrow hits get smaller sparks and a light shake.
- `PrototypeDungeonDressing` dresses each generated floor in a sewer style with the KayKit Dungeon Pack: hex stone, rocky, and grate floor tiles; plain, cracked, and broken stone walls tinted green-grey; and torches with warm point lights on about 8% of walls. The original floor and wall cubes keep their colliders and names, so gameplay is unchanged. Doors, stairs, chests, items, traps, and Goo are still primitive shapes. See `Docs/KAYKIT_HANDOFF.md`.

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
