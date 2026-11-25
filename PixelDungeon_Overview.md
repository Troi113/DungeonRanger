# Pixel Dungeon: Code and Gameplay Overview

You have the original Watabou Pixel Dungeon source code, not Shattered Pixel Dungeon. The repository describes it as a traditional pixel-art roguelike and depends on the separate `PD-classes`/Noosa engine library for rendering and input. ([README.md](/Users/user/Downloads/pixel-dungeon-master/README.md:1))

The project contains about 505 Java files. The important idea is:

```text
PixelDungeon
 └─ Scenes
     └─ GameScene
         └─ Dungeon
             ├─ Hero
             ├─ Current Level
             │   ├─ Terrain map
             │   ├─ Mobs
             │   ├─ Item heaps
             │   ├─ Plants
             │   └─ Blobs/gases/fire
             └─ Statistics, quests, badges, saves
```

## 1. Core game state

### `PixelDungeon`

The Android application entry point. It starts scenes, loads settings, and switches between menus and gameplay.

### `Dungeon`

This is the central run manager. It stores:

- The current `Hero`
- The current `Level`
- Current depth
- Gold
- Challenges
- Visible cells
- Dropped items
- Quest state
- Global progression

It creates the dungeon floor-by-floor and decides which level class to instantiate. ([Dungeon.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/Dungeon.java:73))

### `Statistics`

Tracks:

- Deepest floor
- Experience
- Enemies killed
- Gold collected
- Amulet obtained
- Victory or death
- Special achievements

### Save-related classes

- `GamesInProgress`: saved runs
- `Bundle`: serialization format from the external engine
- `Bones`: remnants of dead heroes
- `Rankings`: completed and failed runs
- `Badges`: achievements
- `Journal`: discovered lore and information

## 2. Turn system

### `Actor`

`Actor` is the base class for anything that acts over time. It implements a simple speed-based scheduler:

```text
Actor
 ├─ Hero
 ├─ Mob
 ├─ Buff
 └─ Blob
```

Each actor has a time value. The actor with the earliest available time acts first. Moving, attacking, eating, reading, and waiting all consume time. ([Actor.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/actors/Actor.java:31))

This makes the game turn-based even though animations appear continuous.

### `Char`

`Char` extends `Actor` and represents living creatures.

It contains:

- Position
- HP and maximum HP
- Name
- Movement speed
- Visibility range
- Status flags
- Buffs
- Sprite
- Combat methods
- Resistances and immunities

([Char.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/actors/Char.java:59))

## 3. Characters

### `Hero`

`Hero` is the player character. It extends `Char`.

The hero owns:

- Weapon
- Armor
- Two rings
- Backpack
- Quick-slot items
- Strength
- Experience and level
- Hero class
- Hero subclass
- Hunger
- Buffs
- Ranged weapon

([Hero.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/actors/hero/Hero.java:115))

### Hero classes

There are four main classes. ([HeroClass.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/actors/hero/HeroClass.java:39))

| Class | Main identity |
|---|---|
| Warrior | Strong melee fighter; food heals him; starts with extra Strength |
| Mage | Wand specialist; wands recharge faster; food recharges wands |
| Rogue | Stealth and evasion; starts with a Ring of Shadows |
| Huntress | Ranged fighter; starts with a boomerang; benefits from plants and dewdrops |

### Hero subclasses

After obtaining and reading the Tome of Mastery, each class chooses one specialization. ([HeroSubClass.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/actors/hero/HeroSubClass.java:22))

```text
Warrior
 ├─ Gladiator: melee combo attacks
 └─ Berserker: increased damage while badly wounded

Mage
 ├─ Battlemage: wand-powered melee combat
 └─ Warlock: healing and hunger restoration after kills

Rogue
 ├─ Assassin: stronger surprise attacks
 └─ Freerunner: faster movement and increased evasion

Huntress
 ├─ Sniper: ranged attacks ignore armor
 └─ Warden: improved plant and dewdrop benefits
```

### `Mob`

`Mob` is the base class for monsters. It extends `Char`.

Mobs contain:

- HP
- Damage
- Defense
- Experience reward
- Loot
- Target
- Hostility
- AI state
- Sprite class
- Resistances and immunities

The built-in AI states are:

- Sleeping
- Wandering
- Hunting
- Fleeing
- Passive

([Mob.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/actors/mobs/Mob.java:41))

### Monsters

The major monster classes include:

- Rat, Albino
- Gnoll
- Crab
- Swarm
- Skeleton
- Thief, Bandit
- Shaman
- Bat
- Brute, Shielded
- Spinner
- Elemental
- Monk, Senior
- Warlock
- Golem
- Succubus
- Eye
- Scorpio, Acidic
- Statue
- Mimic
- Piranha
- Wraith
- Ghost
- Bees and mirror images

`Bestiary` selects monsters based on the dungeon depth. ([Bestiary.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/actors/mobs/Bestiary.java:24))

### Bosses

| Floor | Boss |
|---:|---|
| 5 | Goo |
| 10 | Tengu |
| 15 | DM-300 |
| 20 | King of Dwarves |
| 25 | Yog-Dzewa |

Yog also creates Rotting Fists, Burning Fists, and larvae.

### NPCs

NPCs are special mobs with interaction instead of normal combat:

- Sad Ghost
- Old Wandmaker
- Troll Blacksmith
- Ambitious Imp
- Shopkeeper
- Rat King
- Golden Bee
- Mirror Image

They provide quests, equipment upgrades, shops, or special rewards.

## 4. Status effects

### `Buff`

A `Buff` is an `Actor` attached to a `Char`. Buffs change a character's behavior, statistics, or condition. ([Buff.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/actors/buffs/Buff.java:24))

Important buffs include:

- `Hunger`
- `Regeneration`
- `Poison`
- `Bleeding`
- `Burning`
- `Frost`
- `Paralysis`
- `Blindness`
- `Sleep`
- `Slow`
- `Cripple`
- `Weakness`
- `Terror`
- `Charm`
- `Amok`
- `Invisibility`
- `Levitation`
- `MindVision`
- `Light`
- `Roots`
- `Barkskin`
- `Fury`
- `Combo`
- `SnipersMark`
- `GasesImmunity`

Buffs are attached with calls such as:

```java
Buff.affect(hero, Poison.class);
Buff.detach(hero, Poison.class);
```

## 5. Levels and terrain

### `Level`

`Level` represents one dungeon floor. It contains:

- A 32 × 32 tile map
- Entrance and exit
- Terrain flags
- Mobs
- Item heaps
- Plants
- Blobs
- Visibility data
- Respawn logic
- Level feeling

([Level.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/levels/Level.java:71))

### Level hierarchy

```text
Level
 ├─ RegularLevel
 │   ├─ SewerLevel
 │   ├─ PrisonLevel
 │   ├─ CavesLevel
 │   ├─ CityLevel
 │   ├─ HallsLevel
 │   └─ LastShopLevel
 ├─ SewerBossLevel
 ├─ PrisonBossLevel
 ├─ CavesBossLevel
 ├─ CityBossLevel
 ├─ HallsBossLevel
 ├─ LastLevel
 └─ DeadEndLevel
```

### Terrain

Terrain is stored as integer cell values with bit flags such as:

- Passable
- Solid
- Blocks line of sight
- Flammable
- Liquid
- Pit
- Secret
- Avoid

Terrain types include:

- Empty floor
- Walls
- Doors
- Locked doors
- Water
- Grass
- High grass
- Chasms
- Stairs
- Wells
- Bookshelves
- Altars
- Pedestals
- Traps
- Secret doors

([Terrain.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/levels/Terrain.java:20))

### Level generation

`RegularLevel` generates random rooms, corridors, doors, special rooms, traps, items, and monsters.

`Room` represents a rectangular room. `Painter` subclasses convert rooms into actual terrain:

- Shop
- Armory
- Treasury
- Library
- Garden
- Crypt
- Laboratory
- Vault
- Blacksmith
- Magic well
- Trap room
- Statue room

The dungeon is therefore generated from connected rooms rather than from a fixed map.

## 6. Environmental entities

### `Blob`

A `Blob` is a cell-based area effect. It is not a creature. It occupies multiple map cells and acts over time.

Examples:

- Fire
- Toxic gas
- Paralytic gas
- Confusion gas
- Freezing
- Regrowth
- Foliage
- Web
- Water of Health
- Water of Awareness
- Sacrificial fire
- Alchemy effects

([Blob.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/actors/blobs/Blob.java:30))

### `Plant`

Plants exist on map cells and can be activated by stepping on them or throwing seeds.

Examples:

- Firebloom
- Icecap
- Sorrowmoss
- Dreamweed
- Sungrass
- Earthroot
- Fadeleaf
- Rotberry

`Plant.Seed` is an `Item`, while the resulting plant is a world object. ([Plant.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/plants/Plant.java:43))

### Traps

Traps are represented by terrain values and activated by trap classes:

- Fire trap
- Poison trap
- Toxic trap
- Paralytic trap
- Alarm trap
- Lightning trap
- Gripping trap
- Summoning trap

Secret traps initially look like normal floor.

## 7. Items

### Base hierarchy

```text
Item
 ├─ EquipableItem
 │   ├─ Armor
 │   ├─ Ring
 │   └─ KindOfWeapon
 │       ├─ Weapon
 │       │   ├─ MeleeWeapon
 │       │   └─ MissileWeapon
 │       └─ Wand
 ├─ Potion
 ├─ Scroll
 ├─ Food
 ├─ Key
 ├─ Bag
 ├─ Gold
 ├─ Plant.Seed
 └─ Quest items
```

([Item.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/items/Item.java:50))

### Weapons

Melee weapons include:

- Dagger
- Knuckles
- Quarterstaff
- Spear
- Mace
- Sword
- Longsword
- Battle Axe
- War Hammer
- Glaive

Missile weapons include:

- Darts
- Javelins
- Shurikens
- Boomerang
- Tomahawk
- Curare darts
- Incendiary darts

Weapons can have:

- Upgrade levels
- Enchantments
- Durability
- Accuracy modifiers
- Attack speed
- Strength requirements

### Armor

Armor has:

- Strength requirement
- Damage reduction
- Upgrade level
- Glyph
- Durability
- Class armor variants

Armor glyphs include effects such as:

- Fire resistance
- Repair
- Ricochet
- Displacement
- Entanglement
- Poison
- Evasion

### Wands

Wands provide ranged magic:

- Magic Missile
- Firebolt
- Lightning
- Poison
- Slowness
- Teleportation
- Blink
- Reach
- Flock
- Amok
- Regrowth
- Disintegration
- Avalanche

Wands have limited charges and recharge over time.

### Potions

Potions include:

- Healing
- Strength
- Experience
- Mind Vision
- Invisibility
- Levitation
- Frost
- Purity
- Liquid Flame
- Toxic Gas
- Paralytic Gas
- Might

Potion colors are randomized each run.

### Scrolls

Scrolls include:

- Identify
- Upgrade
- Enchantment
- Remove Curse
- Recharging
- Magic Mapping
- Teleportation
- Terror
- Lullaby
- Challenge
- Psionic Blast
- Mirror Image
- Wipe Out

Scroll symbols are randomized each run.

### Rings

Rings include:

- Accuracy
- Evasion
- Detection
- Haste
- Shadows
- Power
- Elements
- Herbalism
- Mending
- Satiety
- Thorns
- Haggler

Ring gem identities are randomized.

### Bags

- Backpack
- Keyring
- Seed Pouch
- Scroll Holder
- Wand Holster

### Special items

- Gold
- Food
- Torch
- Bomb
- Dew Vial
- Dewdrops
- Ankh
- Tome of Mastery
- Quest items
- Amulet of Yendor

### Identification

`Generator` creates random item types. `ItemStatusHandler` assigns randomized names:

- Potion → color
- Scroll → rune
- Wand → wood type
- Ring → gemstone

The player must identify items by using them, reading identification scrolls, equipping them, or learning class-specific knowledge. ([Generator.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/items/Generator.java:39))

This uncertainty is one of the main roguelike themes.

## 8. Rendering and interface

These classes do not control the game rules; they display them.

### Scenes

- `TitleScene`
- `StartScene`
- `GameScene`
- `InterlevelScene`
- `SurfaceScene`
- `AmuletScene`
- `RankingsScene`
- `BadgesScene`
- `AboutScene`

### Sprites

Sprites visually represent:

- Hero
- Mobs
- Items
- Plants
- Projectiles
- Effects

For example:

```text
Mob → MobSprite
Hero → HeroSprite
Item → ItemSprite
Plant → PlantSprite
```

### UI

The UI package contains:

- Health indicator
- Status pane
- Toolbar
- Quick slots
- Game log
- Buff indicators
- Attack indicator
- Inventory windows
- Item information windows

`GameScene` assembles the map, mobs, plants, item heaps, fog of war, effects, and UI. ([GameScene.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/scenes/GameScene.java:78))

## 9. Dungeon progression

The run has 26 floors:

| Floors | Region | Main boss |
|---:|---|---|
| 1–4 | Sewers | None |
| 5 | Sewer boss floor | Goo |
| 6–9 | Prison | None |
| 10 | Prison boss floor | Tengu |
| 11–14 | Caves | None |
| 15 | Cave boss floor | DM-300 |
| 16–19 | Dwarven City | None |
| 20 | City boss floor | King of Dwarves |
| 21 | Last shop area | None |
| 22–24 | Halls | None |
| 25 | Halls boss floor | Yog-Dzewa |
| 26 | Final room | Amulet of Yendor |

This sequence is implemented in `Dungeon.newLevel()`. ([Dungeon.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/Dungeon.java:151))

## 10. How to win

1. Choose a hero class.
2. Descend through the dungeon.
3. Explore each floor for equipment, potions, scrolls, food, and keys.
4. Defeat the five bosses.
5. Reach floor 26.
6. Pick up the Amulet of Yendor.
7. Choose "Let's call it a day," or return to the surface and ascend.

Picking up the amulet marks the victory achievement. ([Amulet.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/items/Amulet.java:60))

You can also return to floor 1 and ascend. The code then calls `Dungeon.win()` once you have the amulet. ([Hero.java](/Users/user/Downloads/pixel-dungeon-master/src/com/watabou/pixeldungeon/actors/hero/Hero.java:733))

## 11. Practical winning strategy

- Upgrade one main weapon rather than spreading upgrades across many weapons.
- Upgrade armor enough to survive, but keep Strength requirements manageable.
- Save healing potions for emergencies and bosses.
- Use doors and corners to prevent several enemies attacking at once.
- Fight from range whenever possible.
- Avoid unnecessary combat; hunger and attrition are major threats.
- Search suspicious walls, especially as Rogue.
- Use plants, traps, wands, and scrolls instead of relying only on weapon damage.
- Keep at least one escape tool, such as a teleportation scroll or Blink wand.
- Do not blindly equip unknown cursed equipment.
- Preserve upgrade scrolls and strong consumables for later floors.
- Use an Ankh before dangerous boss fights if resurrection is available.
- Pay attention to enemy immunities; magic and elemental attacks do not work equally well against every boss.

## 12. The core game loop

The central relationship is:

```text
Hero action
 → consumes time
 → Actor scheduler advances
 → Buffs and Blobs update
 → Mobs act using AI
 → Combat changes HP/statuses
 → Level state changes
 → GameScene renders the result
```

This is a compact but complete model of how the game's gameplay classes, entities, systems, and relationships fit together.
