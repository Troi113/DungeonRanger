# DungeonRanger TODO

## Project status

The current project contains a runtime-generated 32×32 room with four doorways, a player capsule, two sleeping enemies, WASD movement, eight visible trap markers, straight bow projectiles, and a facing-direction melee slash. The trap markers are visual-only; enemies wake visually when hit but remain stationary.

The goal is to make the game playable first, then improve visuals, content, balance, and presentation.

## Current controls

- [x] `WASD` moves the player.
- [x] Movement updates the player's facing direction.
- [x] `J` fires a straight bow projectile.
- [x] `K` performs a facing-direction melee attack.
- [x] Melee attack has a simple temporary slash animation.
- [x] Basic enemy targets can receive damage and be destroyed.
- [ ] Decide the exact `L` inventory or quick-slot behavior.
- [ ] Add an on-screen control reference for testing.
- [ ] Decide whether inventory pauses live gameplay.

## Immediate prototype stabilization

- [ ] Manually test `J` in each facing direction.
- [ ] Manually test `K` against an enemy directly in front of the player.
- [ ] Test that `K` does not damage enemies behind or beside the player.
- [ ] Test that projectiles collide with enemies and disappear correctly.
- [ ] Test that projectiles stop or disappear when they hit walls.
- [ ] Test room-boundary collision from every direction.
- [ ] Add simple hit feedback to enemies.
- [ ] Add a simple enemy health indicator or temporary damage flash.
- [x] Add two sleeping enemies that wake when hit.
- [ ] Add a simple player health value.
- [ ] Add player death and restart behavior.
- [ ] Replace runtime-only prototype setup with an intentional prototype scene or prefab structure.
- [ ] Remove temporary prototype shortcuts that cause unnecessary per-frame searches.
- [ ] Add a basic debug label showing player position and facing direction.

## Project organization

- [x] Create `AGENTS.md` with project rules.
- [x] Create `TODO.md`.
- [ ] Fill `Docs/GAME_DESIGN.md` with the approved game concept.
- [ ] Fill `Docs/ROADMAP.md` with milestone dates and priorities.
- [ ] Fill `Docs/ART_BIBLE.md` with the visual direction.
- [ ] Fill `Docs/DECISIONS.md` with major technical and design decisions.
- [ ] Update `README.md` with setup, controls, and how to run the project.
- [ ] Add a Unity-appropriate `.gitignore` if version control is configured.
- [ ] Configure Git LFS for large binary assets if needed.
- [ ] Confirm that generated folders such as `Library`, `Temp`, `Logs`, and `UserSettings` are not submitted as source files.

## Playable game foundation

### Player and ranger

- [ ] Create a proper ranger player prefab.
- [ ] Separate player movement, facing, health, combat, and inventory responsibilities.
- [ ] Add player health and maximum health.
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
- [ ] Add enemy wandering behavior.
- [ ] Add enemy detection radius and line-of-sight checks.
- [ ] Add enemy chase behavior.
- [ ] Add enemy attack windup and cooldown.
- [ ] Add enemy hit reactions.
- [ ] Add enemy death feedback and cleanup.
- [ ] Add a simple melee enemy.
- [ ] Add a simple ranged enemy.
- [ ] Add at least one enemy that creates a positioning challenge for the ranger.
- [ ] Add enemy experience or score rewards.
- [ ] Add encounter limits so the player is not attacked by the entire level at once.

### Combat and live-play rules

- [ ] Establish a shared damage and health system.
- [ ] Add combat cooldowns that feel good in real time.
- [ ] Add projectile collision with walls, terrain, and enemies.
- [ ] Add clear attack telegraphs for enemies.
- [ ] Add damage numbers or another readable hit indicator.
- [ ] Add a temporary invulnerability window after player damage.
- [ ] Add simple knockback or stagger only if it improves combat.
- [ ] Add optional auto-targeting later only if straight shooting feels insufficient.
- [ ] Ensure attacks do not create excessive allocations or searches during play.

## Procedural dungeon generation

### Generator foundation

- [ ] Define a logical grid representation for the dungeon.
- [ ] Define terrain data for floor, wall, door, water, grass, trap, entrance, and exit.
- [ ] Implement recursive room splitting or a comparable constrained room generator.
- [ ] Generate a room graph.
- [ ] Connect rooms with corridors.
- [ ] Guarantee a path from entrance to exit.
- [ ] Add extra connections and loops for live-play retreat routes.
- [ ] Add a deterministic seed for every generated run.
- [ ] Add a way to display or log the active seed during testing.
- [ ] Add a post-generation validation pass.
- [ ] Reject or regenerate layouts that are impossible or unfair.

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

- [x] Add one visible prototype marker for each original Pixel Dungeon trap type.
- [ ] Add room painters for standard rooms.
- [ ] Add room painters for treasure rooms.
- [ ] Add room painters for trap rooms.
- [ ] Add room painters for healing or recovery rooms.
- [ ] Add room painters for shops or optional reward rooms if time allows.
- [ ] Add water patches.
- [ ] Add grass or concealment patches.
- [ ] Add decorations after the gameplay layout works.
- [ ] Add functional visible and secret traps with fair telegraphing.
- [ ] Add real-time fire, gas, or hazard behavior only after the core game works.
- [ ] Add line-of-sight and visibility data for walls and obstacles.

## Level progression

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
- [ ] Add victory progression after the boss is defeated.

## Inventory and items

- [ ] Define the inventory model.
- [ ] Define the `L` key behavior.
- [ ] Add an inventory UI.
- [ ] Add weapon slot and armor slot if needed.
- [ ] Add bow and arrow data.
- [ ] Add healing item.
- [ ] Add one utility item.
- [ ] Add one ranger-specific item or ability.
- [ ] Add item pickup and drop behavior.
- [ ] Add item persistence between scenes.
- [ ] Keep the first item system small; add Pixel Dungeon-style complexity later.
- [ ] Decide whether unidentified items are part of the final game.

## User interface and assignment requirements

### Opening and closing UI

- [ ] Create a title screen.
- [ ] Add a start game button.
- [ ] Add a settings button or settings panel.
- [ ] Add a quit or close-game option appropriate for the target platform.
- [ ] Create a game-over screen.
- [ ] Create a victory screen.
- [ ] Add restart and return-to-title actions.

### In-game UI

- [ ] Add player health display.
- [ ] Add score display.
- [ ] Add timer or level display.
- [ ] Add enemy count or encounter status.
- [ ] Add bow cooldown or ammunition display if needed.
- [ ] Add inventory/quick-slot display.
- [ ] Add readable controls and feedback.

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
