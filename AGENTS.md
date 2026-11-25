## Project purpose

DungeonRanger is a Unity 6 Universal Render Pipeline game inspired by the original Watabou Pixel Dungeon. It is a real-time, ranger-only dungeon crawler for a class project.

The current priority is playability first. Use primitive visuals and simple systems until the core loop works. Polish, generated art, audio, and extra content come afterward.

## Current project state

- Unity project: `DungeonRanger`
- Main test scene: `Assets/Scenes/SampleScene.unity`
- Saved prototype scene: `Assets/Scenes/TrapsSleep.unity` (when present)
- Full-floor generator test: `Assets/Scenes/LevelTest.unity` (when present)
- Prototype scripts: `Assets/Scripts/`
- The current room, player, camera, and lighting are generated at runtime by `PrototypeBootstrap`.
- `PrototypeTerrainGenerator` currently generates a procedural 32×32 room with four doorways, floor, and walls.
- `PrototypeLevelGenerator` generates a seeded 32×32 floor from several rectangular rooms and connecting corridors, then builds walls, visible door panels/triggers at room boundaries and corridor junctions, traps, player spawn data, and enemy spawn data. Its logical terrain tile is currently 4 Unity world units wide; trap markers remain approximately 1×1 world unit.
- The current terrain test has two sleeping enemies, no stairs or cliffs, and one visible marker for each original Pixel Dungeon trap type.
- Enemies are stationary for now and wake visually when hit by the player.

Current controls:

- `WASD`: move the player and update the direction the player is facing.
- `Shift`: tumble/dash about one enlarged terrain tile in the facing direction. The dash is collision-aware and has a short cooldown.
- `J`: fire a straight bow projectile in the facing direction.
- `K`: perform a melee attack in the facing direction and show a temporary slash effect.
- `L`: reserved for a future inventory or quick-slot action; do not implement it until the control is defined.

Auto-targeting is intentionally deferred. Bow shots currently travel straight.

## Design direction

Use the original Watabou Pixel Dungeon source as an architectural and gameplay reference, not as code to translate line-by-line. The Unity implementation should use C#, Unity scenes, GameObjects, prefabs, ScriptableObjects where useful, and the Input System.

The planned game is:

- Real-time WASD movement and combat.
- One ranger class only.
- Four procedural dungeon levels and a fifth boss level.
- A procedural room-and-corridor dungeon inspired by Pixel Dungeon.
- A separate boss arena or strongly constrained boss layout.
- Original game identity, visuals, names, and content.

Respect the original Pixel Dungeon project's license and attribution requirements if any source, asset, or design is reused. Prefer new Unity code and original assets.

## Engineering conventions

- Use the Unity Input System rather than the legacy `Input` API.
- Keep gameplay rules separate from presentation and temporary prototype setup.
- Keep scripts small and focused: movement, combat, enemies, generation, UI, persistence, and scene flow should not become one large script.
- Keep scripts under `Assets/Scripts/` and organize future systems into clear subfolders.
- Prefer serialized fields for tuning values that should be editable in the Inspector.
- Use deterministic random seeds for procedural generation once the generator is added.
- Do not add assets or systems merely for polish before the game is playable.
- Do not hand-edit Unity scene YAML unless there is a clear reason. Prefer the Unity Editor or dedicated editor scripts.
- Avoid per-frame searches and allocations in finished systems. Prototype shortcuts are acceptable temporarily, but record or replace them before final submission.
- Keep the project compatible with Unity 6 and URP.

## AI collaboration rules

- Read the relevant files before editing them.
- One coding agent should modify a given set of files at a time.
- Make small, testable changes and explain which files changed.
- Do not silently replace working systems with a different architecture.
- When a feature is incomplete, mark it in `TODO.md` instead of pretending it is finished.
- After code changes, compile and playtest in Unity when possible.
- Report compile errors, warnings, and unverified behavior honestly.
- Keep the prototype playable after every milestone.

## Verification checklist

For gameplay changes:

1. Open `SampleScene`.
2. Enter Play mode.
3. Confirm the room and player appear.
4. Test the relevant keyboard controls.
5. Check collision and behavior at room boundaries.
6. Check the Unity Console for errors.

For procedural-generation changes:

1. Test multiple seeds.
2. Confirm the entrance and exit are connected.
3. Confirm the player has a safe starting area.
4. Confirm the generated layout is playable in real time.

The current `LevelTest` uses seed `4582`, six attempted rooms, one-cell corridors,
visible doors, visible trap markers, and sleeping enemies. It is a first playable
layout prototype; entrance/exit, validation, and multiple-seed testing are not
implemented yet.

For submission work:

- Verify opening and closing UI.
- Verify persistent high score and settings.
- Verify at least three playable levels or scenes.
- Verify health or score persists between levels.
- Verify the Windows build.
- Verify the source archive and demonstration video.

## Definition of done

A feature is not done until it works in Unity Play mode, does not introduce console errors, is documented in `TODO.md` or the appropriate design document, and remains compatible with the current project direction.
