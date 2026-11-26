# KayKit integration – handoff notes

Status of the KayKit asset integration (Ranger hero + sewer dungeon dressing). Read this to continue the work.

## Done (compiles; see "Verified" for what was checked in Play mode)

- Assets copied (CC0, Kay Lousberg) to `Assets/ThirdParty/Resources/KayKit/`:
  - `Characters/`: `Ranger.fbx`, `ranger_texture.png`, `Rig_Medium_General.fbx` (Idle_A/B, Death_A/B, Hit_A/B, Throw, PickUp, Interact, …), `Rig_Medium_MovementBasic.fbx` (Walking_A/B/C, Running_A/B, Jump_*), `License.txt`.
  - `Dungeon/` (from the pack's `fbx(unity)` folder): `floor_tile_large`, `floor_tile_large_rocks`, `floor_tile_big_grate`, `floor_tile_grate`, `wall`, `wall_cracked`, `wall_broken`, `torch_mounted`, `barrel_small` (not used yet), `dungeon_texture.png`, `License.txt`.
  - Source zips: `~/Downloads/KayKit_Adventurers_2.0_FREE.zip`, `~/Downloads/KayKit_Dungeon_Pack_1.1_FREE.zip`. The dungeon pack also has `floor_tile_big_spikes.fbx` (possible trap art), stairs, barrels, crates, banners, pillars.
- `Assets/Scripts/Editor/KayKitImporter.cs`: KayKit models import with no materials; `Characters/` FBX import as Legacy animation; textures point-filtered, no mipmaps.
- `Assets/Scripts/Editor/PixelArtTextureImporter.cs` now only touches the UI pack folder (it used to catch every PNG under ThirdParty).
- `PrototypeKayKit.cs`: loads/caches prefabs, makes URP Lit materials from pack textures, measures prefab bounds.
- `PrototypeHeroModel.cs` (added to the player in `PrototypeBootstrap.CreatePlayer`): hides the capsule renderer, spawns the Ranger scaled to 2.3 units tall with feet on the controller bottom, adds Rig_Medium clips to a Legacy `Animation`, plays `Idle_A` / `Running_A` / `Death_A`, and turns the model to `FacingDirection`.
- `PrototypeDungeonDressing.cs` (called from `PrototypeDungeonRun.BuildFloor` right after generation): hides `Floor_*` and `Wall_*` cube renderers (colliders and names stay, so gameplay is unchanged) and places fitted KayKit pieces: floors 62% plain / 18% rocky / 10% big grate / 10% grate, random 90° turns; walls 75% plain / 20% cracked / 5% broken, scaled to the 4×2.5 wall and 0.6 deep; 8% of walls get a mounted torch with a warm point light on the side facing a floor cell. Seeded per floor. Stone is tinted green-grey for a sewer feel.

## Verified

- Play mode (editor): dungeon floor 1 renders with KayKit hex floor tiles, grates, rocky tiles, green-grey stone walls and a lit wall torch; the Ranger model appears in the room in an idle pose (clips play, not a T-pose). No errors in the log.
- Not checked: running/death animations, facing while moving, boss arena look, performance with torches on all floors.

## Next steps / open issues

1. Play-test running/death clips and facing while moving (if the model faces backwards, add a 180° yaw to the Hero_Model holder).
2. Check wall pieces line up at corners and do not block the camera view; adjust `WallDepth` or height.
3. Torch lights: performance with many point lights on large floors; URP per-object light limit may hide some.
4. Stairs use KayKit `stairs.fbx`: yellow up stairs rise; blue down stairs sink into a pit (`BuildStairPit`, `PrototypeDungeonDressing.ClearFloorAt`). Not yet seen in Play mode. Doors, chest, items, and the boss arena are still primitive shapes; KayKit has `stairs`, `chest`, `barrel`, `key`, `torch_lit`, `wall_doorway` that could replace them.
5. Traps: user will decide; `floor_tile_big_spikes` exists in the pack.
6. Docs (`STATE.md`, `CREDITS.md`, `TODO.md`) were updated for this work.

## Goo update (latest session)

- Done, compiles, NOT seen in Play mode: `PrototypeGooFace.cs` (original primitive slime face, squash and random silly faces on hit via new `PrototypeEnemy.Damaged` event; face renderers follow fog via `PrototypeEnemy.AttachExtraRenderers`), `PrototypeSlimeTrail.cs` (puddles; `PrototypePlayerMovement` slows to 45% and blocks tumble while `IsSlimed`), Goo scale 9×6×9 in `PrototypeMobs`, Goo now spawns at half its height (`PrototypeDungeonRun.CreateEnemy`).
- The user's Poring model (`~/Downloads/poring-ragnarok-online.zip`) was intentionally NOT used: it is Ragnarok Online's character art.
- Next: check Goo fits the boss arena at 3x, face placement on the sphere, charge/knockback at the new size, slime trail balance.
