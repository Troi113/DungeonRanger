# DungeonRanger
Unity 6 URP real-time ranger-only dungeon crawler (class project, Pixel Dungeon-inspired).
Priority: playable first. Primitive visuals; no polish/art/audio until the core loop works.

## Rules
- Use the Input System, never legacy `Input`.
- C#, scripts under `Assets/Scripts/`; use clear subfolders as the project grows. Keep scripts small and single-purpose.
- Keep gameplay logic separate from presentation and prototype setup.
- Tunable values go in `[SerializeField]` fields.
- Procedural generation uses seeded deterministic random.
- Don't hand-edit scene YAML unless necessary.
- Keep the project compatible with Unity 6 and URP.
- No per-frame searches/allocations in finished systems; note prototype shortcuts in TODO.md.
- Don't replace working systems with a new architecture.
- Reusing Pixel Dungeon code/assets requires attribution. Prefer original work.

## Workflow
- Read a file before editing it. Only touch files needed for the task.
- Small, testable changes. List the files you changed.
- Compile and playtest in Unity when possible after code changes.
- Mark unfinished work in TODO.md. Report compile errors and unverified behavior honestly.
- `L` key is reserved. Don't implement it.

## Read only when relevant
- Docs/STATE.md: current scenes, scripts, controls
- Docs/DESIGN.md: planned game scope
- Docs/TESTING.md: verification checklists
- TODO.md: open work
