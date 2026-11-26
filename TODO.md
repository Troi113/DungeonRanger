# DungeonRanger TODO

## Project status

The current project contains two runtime-generated prototypes: `TrapsSleep` is a 32×32 room with four doorways, while `LevelTest` is a seeded 32×32 room-and-corridor floor. Both include a player capsule, sleeping enemies, WASD movement, straight bow projectiles, a facing-direction melee slash, and a health HUD. `LevelTest` also contains functional one-use traps, player health, inventory data, status effects, hunger logic, pickups, and a locked chest. Sleeping enemies wake one at a time and patrol the generated floor.

The goal is to make the game playable first, then improve visuals, content, balance, and presentation.

## Current controls

- [x] `WASD` moves the player.
- [x] Movement updates the player's facing direction.
- [x] `Shift` tumbles/dashes in the player's facing direction with collision handling and a cooldown.
- [x] `J` fires a straight bow projectile.
- [x] Five consecutive `Q` presses toggle a one-hit bow cheat for testing.
- [x] `K` punches unarmed or attacks with an equipped melee weapon in the facing direction.
- [x] Melee hits a full tile in front of the player (two tiles for the slowest weapon, the spear).
- [ ] Playtest the tile-sized melee area; it may hit enemies diagonally close to the player's side.
- [x] Melee attack has a simple temporary slash animation.
- [x] Basic enemy targets can receive damage and be destroyed.
- [ ] Decide the exact `L` inventory or quick-slot behavior.
- [x] Add an on-screen health and hunger HUD.
- [x] Add an on-screen `I` inventory toggle.
- [x] Add an on-screen control hint for inventory.
- [x] Pause gameplay while the inventory is open.
- [x] Add an Escape pause menu with Continue and Quit to Main Menu.

## Immediate prototype stabilization

- [ ] Manually test `J` in each facing direction.
- [ ] Playtest the five-`Q` bow cheat, including interruption, toggle-off, and a one-hit defeat of Goo.
- [ ] Manually test `K` against an enemy directly in front of the player.
- [ ] Test that `K` does not damage enemies behind or beside the player.
- [ ] Test that projectiles collide with enemies and disappear correctly.
- [x] Test that projectiles disappear on walls and closed doors and pass through open doors.
- [ ] Test room-boundary collision from every direction.
- [ ] Add simple hit feedback to enemies.
- [x] Add an enemy health bar that appears after damage and respects fog of war.
- [ ] Add temporary damage flashes or other hit feedback.
- [x] Add two sleeping enemies that wake when hit.
- [x] Add a simple player health value with a 20-health prototype default.
- [x] Add player death and restart behavior.
- [x] Add a visible player death/game-over state.
- [ ] Replace runtime-only prototype setup with an intentional prototype scene or prefab structure.
- [ ] Remove temporary prototype shortcuts that cause unnecessary per-frame searches.
- [ ] Add a basic debug label showing player position and facing direction.

## Project organization

- [x] Create `AGENTS.md` with project rules.
- [x] Create `TODO.md`.
- [x] Create `Docs/DESIGN.md` with the approved game direction.
- [x] Create `Docs/STATE.md` with the current project state.
- [x] Create `Docs/TESTING.md` with verification checklists.
- [ ] Keep the files in `Docs/` synchronized with each milestone.
- [ ] Update `README.md` with setup, controls, and how to run the project.
- [ ] Add a Unity-appropriate `.gitignore` if version control is configured.
- [ ] Configure Git LFS for large binary assets if needed.
- [ ] Confirm that generated folders such as `Library`, `Temp`, `Logs`, and `UserSettings` are not submitted as source files.

## Playable game foundation

### Player and ranger

- [ ] Create a proper ranger player prefab.
- [x] Separate player movement, facing, health, combat, and inventory responsibilities.
- [x] Add player health and maximum health.
- [ ] Add damage, invulnerability frames, and death state.
- [ ] Add bow cooldown and attack feedback.
- [ ] Add ammunition or decide that arrows are unlimited.
- [ ] Add melee damage, range, cooldown, and hit feedback.
- [ ] Decide whether the ranger can move while attacking.
- [ ] Decide whether bow attacks require line of sight.
- [ ] Decide whether bow attacks use mouse aim, facing aim, or a future targeting mode.
- [ ] Add a ranger-specific upgrade path.
- [ ] Keep the first version to one class with no subclasses.

### Enemies

- [ ] Create an enemy base component with health, damage, movement speed, and reward data.
- [ ] Add enemy idle behavior.
- [x] Add randomized delayed wake-up behavior for LevelTest enemies.
- [x] Immediately wake sleeping enemies when they see the player within their attention range or are hit.
- [x] Add walkable-cell patrol behavior for awake LevelTest enemies.
- [x] Add an eight-cell player detection range for LevelTest enemies.
- [x] Add circular 9-cell fog-of-war visibility around the player.
- [x] Keep walls and door frames visible as the map outline.
- [x] Block visibility through closed doors and reveal the area beyond opened doors.
- [x] Track separate visible, visited, mapped, and invisible fog states.
- [x] Add enemy wandering behavior.
- [ ] Add enemy detection radius and line-of-sight checks.
- [x] Add enemy chase behavior.
- [x] Add a close-range enemy attack with a cooldown.
- [ ] Add enemy attack windup and cooldown.
- [ ] Add enemy hit reactions.
- [ ] Add enemy death feedback and cleanup.
- [x] Add rat and albino rat mobs for floors 1–4, plus Goo damage range, defense, and ooze.
- [ ] Show active status effects (bleeding, ooze, poison) on the HUD.
- [ ] Playtest and balance rat/albino/Goo health, damage, defense, and the 1/30 albino chance.
- [ ] Consider Pixel Dungeon-style hit/miss accuracy once there is hit feedback to show misses.
- [ ] TrapsSleep and LevelTest still spawn the old generic enemy (via `PrototypeBootstrap`).
- [ ] Add a simple ranged enemy.
- [ ] Add at least one enemy that creates a positioning challenge for the ranger.
- [ ] Add enemy experience or score rewards.
- [ ] Add encounter limits so the player is not attacked by the entire level at once.

### Combat and live-play rules

- [x] Establish a shared prototype damage and health system.
- [ ] Add combat cooldowns that feel good in real time.
- [ ] Extend projectile collision to additional terrain and objects as they are added.
- [x] Add swept arrow collision against enemies and walls, and slow normal enemies on bow and melee hits.
- [ ] Add clear attack telegraphs for enemies.
- [ ] Add damage numbers or another readable hit indicator.
- [ ] Add a temporary invulnerability window after player damage.
- [ ] Add simple knockback or stagger only if it improves combat.
- [ ] Add optional auto-targeting later only if straight shooting feels insufficient.
- [ ] Ensure attacks do not create excessive allocations or searches during play.

## Procedural dungeon generation

### Generator foundation

- [x] Define a logical grid representation for the dungeon.
- [ ] Define terrain data for floor, wall, door, water, grass, trap, entrance, and exit.
- [ ] Implement recursive room splitting or a comparable constrained room generator.
- [x] Generate a seeded set of non-overlapping rectangular rooms.
- [x] Connect rooms with corridors.
- [x] Place doors at carved room entrances and wall off the remaining room boundaries.
- [x] Support adjacent rooms connected by one door and corridor connections with doors at room entrances.
- [x] Connect all generated rooms and add an alternate route for a loop.
- [x] Route corridors around unrelated rooms instead of cutting through them.
- [x] Retry unsuitable room placements before constructing the floor.
- [x] Add a deterministic seed for the first level test.
- [ ] Add a way to display or log the active seed during testing.
- [ ] Add a post-generation validation pass.
- [ ] Reject or regenerate layouts that are impossible or unfair.

### LevelTest milestone

- [x] Create `Assets/Scenes/LevelTest.unity`.
- [x] Generate a playable 32×32 room-and-corridor floor at runtime.
- [x] Build walls around carved floor cells.
- [x] Add visible doors at room/corridor connections.
- [x] Add opening door panels with room-scoped enemy alert behavior.
- [x] Spawn the player in the first generated room.
- [x] Spawn sleeping enemies in later generated rooms.
- [x] Configure `LevelTest` as dungeon level 4 with a source-inspired 6–8 starting mob range.
- [x] Place the eight visible prototype trap markers in generated floor cells.
- [x] Enlarge LevelTest terrain tiles to four world units while keeping trap markers one world block wide.
- [x] Add generated entrance/exit positions and an exit marker.
- [ ] Add layout validation and unreachable-room detection.
- [x] Check connectivity, unique doors, and alternate routes across 120 seeds.
- [ ] Playtest movement, door transitions, and combat across different layouts and tune dimensions.

### Live-play generation rules

- [ ] Use rooms large enough for bow combat and dodging.
- [ ] Use corridors at least two tiles wide where appropriate.
- [ ] Avoid placing enemies directly on the entrance.
- [ ] Avoid placing unavoidable traps at entrances or in one-tile passages.
- [ ] Guarantee retreat routes near combat encounters.
- [ ] Guarantee enough recovery opportunities per level.
- [ ] Keep the boss arena large enough for ranged movement.
- [ ] Add encounter zones that activate when the player enters a room.
- [ ] Add room-level enemy and loot budgets.

### Terrain and special rooms

- [x] Add eight prototype trap types with equal 1/8 generation probability.
- [x] Allow traps to start visible or hidden.
- [x] Activate traps once when the player steps on them.
- [x] Add prototype effects for damage, damage-over-time, movement blocking, and enemy wake-up.
- [ ] Add room painters for standard rooms.
- [ ] Add room painters for treasure rooms.
- [ ] Add room painters for trap rooms.
- [ ] Add room painters for healing or recovery rooms.
- [ ] Add room painters for shops or optional reward rooms if time allows.
- [ ] Add water patches.
- [ ] Add grass or concealment patches.
- [ ] Add decorations after the gameplay layout works.
- [ ] Improve functional visible and secret traps with fair telegraphing.
- [ ] Add real-time fire, gas, or hazard behavior only after the core game works.
- [ ] Add line-of-sight and visibility data for walls and obstacles.

## Level progression

- [x] Add a separate DungeonGameplay scene with four generated floors and a generated Goo arena.
- [x] Add up/down stair visuals and a Space interaction to descend.
- [x] Allow up stairs to return to previously visited floors while preserving their runtime state.
- [x] Keep player health and inventory while replacing generated floors in the same scene.
- [x] Make Goo drop the Amulet of Glory; picking it up opens a congratulations scene.
- [ ] Playtest all four stair transitions and the Goo victory and restart flows.
- [x] Add Goo's telegraphed, straight-line charge attack.
- [ ] Playtest and balance Goo's charge attack during a full run.
- [ ] Create a four-level procedural progression.
- [ ] Create a fifth boss level.
- [ ] Decide whether the first four levels share a sewer theme or use different themes.
- [ ] Level 1: tutorial layout and basic enemies.
- [ ] Level 2: denser encounters and more environmental variation.
- [ ] Level 3: tougher enemies and stronger hazards.
- [ ] Level 4: elite encounters and reduced recovery.
- [ ] Level 5: boss approach and boss arena.
- [ ] Add level entrances and exits.
- [ ] Add scene or level transition logic.
- [ ] Persist player health between levels.
- [ ] Persist score or progression between levels.
- [ ] Keep a separate boss arena layout or strongly constrain its generation.
- [ ] Implement one original boss with readable attack patterns.
- [ ] Add boss health, phases, and defeat state.
- [x] Add victory progression after the boss is defeated and its amulet is collected.

## Inventory and items

- [x] Define a 20-slot inventory model.
- [x] Reserve weapon, armor, and two artifact/ring equipment slots.
- [x] Add a prototype status-effect component for buffs/debuffs foundation.
- [x] Add prototype hunger states: satiated, hungry, and famished.
- [x] Damage the player over time while famished.
- [x] Add a 4-column by 5-row inventory overlay opened with `I`.
- [x] Add WASD/arrow navigation, `J` item actions, and `Esc` to deselect.
- [x] Start with a permanent bow in a general inventory slot; shooting does not require equipping it.
- [x] Generate melee weapon pickups and allow equipping, swapping, and unequipping them.
- [ ] Playtest and balance melee weapon damage, reach, and cooldown across the generated floors.
- [x] Add cloth, leather, and mail armor pickups (one per floor) that absorb a random 0–(tier + upgrades) damage per enemy hit.
- [x] Give unidentified weapons and armor a chance (default 15%) to become cursed when equipped.
- [ ] Playtest and balance armor absorption against enemy and Goo damage; enemies deal 1, so armor blocks many hits outright.
- [ ] Decide whether heavier armor needs a drawback (Pixel Dungeon uses Strength requirements; DungeonRanger has no Strength yet).
- [ ] Add combat or stat effects for artifacts and rings.
- [x] Add Space-bar item pickup interaction.
- [x] Add a key pickup and locked chest interaction.
- [x] Add generated prototype items to LevelTest.
- [x] Add stackable Challenge, Upgrade, Remove Curse, Teleportation, Identify, and Magic Mapping scrolls.
- [x] Add per-run unidentified rune labels, item selection, curse removal, and melee weapon upgrades.
- [ ] Add a real-time scroll-reading animation or delay if desired; scroll effects currently apply from the paused inventory.
- [x] Extend Upgrade scrolls to armor.
- [ ] Extend upgrade and identification effects to rings and wands when those systems exist.
- [x] Make health potions usable from the inventory.
- [ ] Make equipment slots affect combat or player stats.
- [ ] Add item stack counts and duplicate-item behavior.
- [ ] Add item drop behavior.
- [ ] Define the `L` key behavior.
- [x] Add weapon slot and armor slot placeholders.
- [ ] Add bow and arrow data.
- [x] Add a prototype healing item.
- [ ] Add one utility item.
- [ ] Add one ranger-specific item or ability.
- [x] Add item pickup behavior.
- [ ] Add item drop behavior.
- [ ] Add item persistence between scenes.
- [ ] Keep the first item system small; add Pixel Dungeon-style complexity later.
- [ ] Decide whether unidentified items are part of the final game.

## User interface and assignment requirements

### Opening and closing UI

- [x] Create a title screen.
- [x] Add a start game button.
- [ ] Add a settings button or settings panel.
- [x] Add a quit or close-game option appropriate for the target platform.
- [ ] Create a game-over screen.
- [x] Create a prototype victory screen with a return-to-menu option.
- [ ] Add restart and return-to-title actions.

### In-game UI

- [x] Add player health display.
- [ ] Add score display.
- [ ] Add timer or level display.
- [ ] Add enemy count or encounter status.
- [ ] Add bow cooldown or ammunition display if needed.
- [x] Add inventory/quick-slot display.
- [ ] Add readable controls and feedback.
- [x] Restyle the IMGUI HUD, inventory, pause, death, main menu, and victory screens with the medieval pixel UI pack.
- [ ] Playtest the restyled inventory, item-action popup, pause menu, and death screen (not yet viewed in Play mode).
- [ ] Replace approximate item icons (sickle, cloth armor, leather armor) if better art is found.
- [ ] Check the UI colors in a standalone build (the Linear color-space correction was verified only in the editor).

### Persistence

- [ ] Add persistent high score.
- [ ] Add persistent user settings.
- [ ] Add persistent audio and control settings if used.
- [ ] Add player health persistence between scenes.
- [ ] Add score persistence between scenes.
- [ ] Test persistence after closing and reopening the game.
- [ ] Use a clear save format and keep save logic separate from gameplay logic.

## Visuals, audio, and feel

- [ ] Choose the final camera style.
- [ ] Choose the final pixel-art or stylized 3D presentation.
- [ ] Create a consistent palette.
- [ ] Replace primitive player model with an original ranger asset.
- [ ] Replace primitive enemies with original enemy assets.
- [ ] Create dungeon floor and wall assets.
- [ ] Create bow, arrow, and melee visual assets.
- [ ] Improve slash animation.
- [ ] Add projectile trail or impact effect.
- [ ] Add enemy death effects.
- [ ] Add player damage feedback.
- [ ] Add ambient dungeon audio.
- [ ] Add bow, melee, hit, pickup, and UI sounds.
- [ ] Add lighting and atmosphere after the level layouts are stable.
- [ ] Record asset sources and credits in the project documentation.

## Balance and testing

- [ ] Create a repeatable test checklist.
- [ ] Test at least ten procedural seeds.
- [ ] Test that every seed has a reachable exit.
- [ ] Test that the player cannot leave the room or level boundaries incorrectly.
- [ ] Test bow combat at short, medium, and long range.
- [ ] Test melee combat against multiple enemies.
- [ ] Test enemy difficulty progression.
- [ ] Test health and score persistence.
- [ ] Test title, game-over, victory, and restart flows.
- [ ] Test settings persistence.
- [ ] Test the game with a fresh save.
- [ ] Test the game after closing and reopening Unity.
- [ ] Test performance on the M2 MacBook Air.
- [ ] Test a Windows build on an available Windows machine or environment.

## Submission requirements

- [ ] Confirm the final game satisfies the opening and closing UI requirement.
- [ ] Confirm persistent high score and user settings.
- [ ] Confirm challenging but achievable gameplay.
- [ ] Confirm in-game time, score, or enemy UI.
- [ ] Confirm at least three levels or scenes.
- [ ] Confirm health or score persists between scenes.
- [ ] Confirm original look and feel.
- [ ] Confirm meaningful programming complexity.
- [ ] Create a clean source-project zip.
- [ ] Create a complete Windows build.
- [ ] Zip the Windows build folder.
- [ ] Record the demonstration video.
- [ ] In the video, demonstrate each grading requirement.
- [ ] Upload the source zip, Windows build zip, and video to Canvas or approved cloud storage.
- [ ] Verify that all submission links work before the deadline.

## Deferred or intentionally out of scope for now

- [ ] Auto-targeting bow attacks.
- [ ] Multiple hero classes.
- [ ] Hero subclasses.
- [ ] The original 26-floor progression.
- [ ] Multiple bosses.
- [ ] Large NPC quest systems.
- [ ] Full Pixel Dungeon item-identification complexity.
- [ ] Extensive procedural environmental effects.
- [ ] Final art polish before the playable loop is complete.
