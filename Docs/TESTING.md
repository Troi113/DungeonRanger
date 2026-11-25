# Testing Checklists

## Gameplay changes
1. Open the scene relevant to the change:
   - `SampleScene` for the basic room and general movement/combat.
   - `TrapsSleep` for traps, sleeping enemies, arrows, and door behavior.
   - `LevelTest` for the procedural room-and-corridor floor.
2. Enter Play mode.
3. Confirm the room and player appear.
4. Test the relevant keyboard controls.
5. Check collision and behavior at room boundaries.
6. Check the Unity Console for errors.

For code changes, also confirm that Unity finishes compiling without errors before entering Play mode.

## Procedural-generation changes
1. Test multiple seeds.
2. Once entrance and exit systems exist, confirm they are connected.
3. Confirm the player has a safe starting area.
4. Confirm the layout is playable in real time.

## Submission
- Opening and closing UI work.
- High score and settings persist.
- At least three playable levels or scenes.
- Health or score persists between levels.
- Windows build verified.
- Source archive and demonstration video verified.

## Definition of done
Works in Play mode, no console errors, logged in `TODO.md` or the relevant design doc, and compatible with the project direction.
