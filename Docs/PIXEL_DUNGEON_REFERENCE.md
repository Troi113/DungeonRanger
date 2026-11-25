# Pixel Dungeon Reference Map

This document records the useful architectural ideas taken from the original Watabou Pixel Dungeon source in `/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/`. It is a design reference, not a line-by-line port.

## Source concepts used by DungeonRanger

| Pixel Dungeon concept | Source area | DungeonRanger direction |
|---|---|---|
| 32×32 terrain grid | `levels/Level.java` | `PrototypeLevelGenerator` keeps a seeded 32×32 logical grid with enlarged Unity tiles. |
| Entrance and exit rooms | `levels/RegularLevel.java` | LevelTest selects the first and last generated rooms and validates connectivity. |
| Room graph and painters | `levels/Room.java`, `levels/painters/` | Future special rooms should be added as constrained room-generation strategies, not random decorations scattered across the map. |
| Mobs and detection | `actors/mobs/`, `actors/Char.java` | Unity enemies patrol in real time, detect within eight dungeon cells, chase, and attack at close range. |
| Fog states | `FogOfWar.java` and `Level.java` | Unity tracks visible, visited, mapped, and invisible areas while preserving wall/door outlines. |
| Trap terrain and effects | `levels/RegularLevel.java`, `levels/traps/`, `actors/blobs/` | Unity traps are one-use components with visible/hidden states and source-inspired effect categories. |
| Item heaps and chests | `items/Heap.java`, `items/Item.java` | Unity currently uses individual pickups and one locked chest; stacking and drop piles are future work. |
| Hunger | `actors/buffs/Hunger.java`, `items/food/Food.java` | Unity uses a real-time moving-time hunger meter and food interaction rather than turn advancement. |

## Important adaptations

- Pixel Dungeon advances actors through a turn scheduler. DungeonRanger is live-play, so movement, attacks, hunger, status effects, and enemy AI run through Unity updates and cooldowns.
- Pixel Dungeon uses terrain integer flags. DungeonRanger currently uses generated GameObjects and a walkable-cell lookup; terrain flags should be added when water, grass, secret doors, and special rooms become real gameplay systems.
- Pixel Dungeon's inventory is a bag with stackable items and action menus. DungeonRanger currently has 20 fixed slots and an immediate-mode inventory overlay; item stacking, dropping, and richer actions remain intentionally deferred.
- Pixel Dungeon's traps often create timed blobs or buffs. DungeonRanger currently represents the first pass with direct damage and timed status effects; persistent area hazards can be added after the basic combat loop is stable.

## Attribution boundary

The original source is licensed under the GNU General Public License. DungeonRanger uses it for study and design reference while keeping the Unity implementation and game identity original. Any future reused source or asset must be reviewed for license compatibility and documented before inclusion.
