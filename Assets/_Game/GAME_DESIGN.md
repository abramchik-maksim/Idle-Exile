# Idle Exile — Game Design Document

## Game Overview

**Genre:** 2D Idle RPG
**Platform:** PC (Unity 6, URP)
**Core Loop:** Automatic combat → loot → equip gear → progress through tiers

The player watches an auto-battling hero fight waves of enemies on the left third of the screen, while managing equipment, skills, and stats on the right two-thirds via tabbed UI panels.

---

## Screen Layout

```
┌───────────────────┬────────────────────────────────────────┐
│                   │  [Character]  [Equipment]  [Skills]    │
│   Combat Area     │────────────────────────────────────────│
│   (left 1/3)      │                                        │
│                   │       Active Tab Content (2/3)         │
│   Hero ●          │                                        │
│   Enemies ▼▼▼     │                                        │
│   Projectiles →   │                                        │
│                   │                                        │
│ ┌───────────────┐ │                                        │
│ │ ■  □ □ □ □    │ │                                        │
│ │ Skill Slots   │ │                                        │
│ └───────────────┘ │                                        │
└───────────────────┴────────────────────────────────────────┘
```

---

## Stat System

### Hero Stats

| Stat               | Base Value | Description                                |
|--------------------|------------|--------------------------------------------|
| Max Health         | 100        | Maximum hit points                         |
| Current Health     | 100        | Current hit points                         |
| Physical Damage    | 10         | Base damage per attack                     |
| Attack Speed       | 1.0        | Attacks per second                         |
| Critical Chance    | 5%         | Chance to deal a critical hit              |
| Critical Multiplier| 150%       | Damage multiplier on critical hit          |
| Armor              | 5          | Flat physical damage reduction             |
| Evasion            | 0          | Chance to dodge incoming attacks           |
| Movement Speed     | 1          | Hero movement speed                        |
| Health Regen       | 1          | Health regenerated per second              |

### Modifier Types

Stats are modified by two types of modifiers:
- **Flat** — adds a flat value (e.g., +15 Physical Damage)
- **Increased** — percentage-based increase (e.g., +20% Attack Speed)

Modifiers come from equipped items (implicit + rolled) and are recalculated when equipment changes.

---

## Item System

### Rarity Tiers

| Rarity  | Color     | Modifiers        | Visual                          |
|---------|-----------|------------------|---------------------------------|
| Normal  | White     | Implicit only    | Standard border                 |
| Magic   | Blue      | + 1–2 rolled     | Blue border + background tint   |
| Rare    | Yellow    | + 3–4 rolled     | Yellow border + glow effect     |
| Unique  | Orange    | Fixed special set | Orange border + glow effect     |

### Equipment Slots (10 total)

| Slot         | Type    | Notes                                       |
|--------------|---------|---------------------------------------------|
| Helmet       | Armor   | Head protection                              |
| Body Armor   | Armor   | Chest protection                             |
| Gloves       | Armor   | Hand protection                              |
| Boots        | Armor   | Foot protection, often has Movement Speed    |
| Amulet       | Jewelry | Offensive/defensive stats                    |
| Belt         | Jewelry | Defensive utility                            |
| Ring × 2     | Jewelry | Two ring slots for stat stacking             |
| Main Hand    | Weapon  | Primary weapon (damage source)               |
| Off Hand     | Weapon  | Secondary weapon or shield                   |

### Weapon Handedness

| Handedness  | Behavior                                                    |
|-------------|-------------------------------------------------------------|
| Versatile   | Can equip in Main Hand or Off Hand                          |
| Two-Handed  | Occupies Main Hand, blocks Off Hand slot (visually dimmed)  |
| Off Hand    | Can only be placed in Off Hand slot                         |

### Weapon Types

| Type   | Associated Main Skills |
|--------|------------------------|
| Bow    | Arrow Shot             |
| Sword  | (future)               |
| Axe    | (future)               |
| Staff  | (future)               |
| Dagger | (future)               |

### Current Items

| Item ID        | Name          | Slot      | Rarity | Weapon Type | Handedness | Key Stats                      |
|----------------|---------------|-----------|--------|-------------|------------|--------------------------------|
| basic_bow      | Basic Bow     | MainHand  | Normal | Bow         | Versatile  | +10 Phys Dmg, +0.5 Atk Spd    |
| iron_sword     | Iron Sword    | MainHand  | Normal | Sword       | Versatile  | +15 Phys Dmg                   |
| iron_saber     | Iron Saber    | MainHand  | Magic  | Sword       | Versatile  | +12 Phys Dmg, +0.3 Atk Spd    |
| super_sword    | Super Sword   | MainHand  | Rare   | Sword       | Versatile  | +25 Phys Dmg, +1.0 Atk Spd    |
| iron_helmet    | Iron Helmet   | Helmet    | Normal | —           | —          | +5 Armor                       |
| leather_vest   | Leather Vest  | BodyArmor | Normal | —           | —          | +8 Armor                       |
| worn_gloves    | Worn Gloves   | Gloves    | Normal | —           | —          | +3 Armor                       |
| simple_boots   | Simple Boots  | Boots     | Normal | —           | —          | +2 Armor, +0.5 Move Spd        |

### Loot System

Items drop from defeated enemies during combat:
- **Base drop chance:** 30% + 2.5% per battle index (capped at 65%)
- **Bonus per tier:** +10% per tier
- **Modifier rolls:** Random values between 1–10
- Items roll random modifiers based on rarity tier

### Item Interactions

| Action              | Trigger                  | Result                              |
|---------------------|--------------------------|-------------------------------------|
| Equip (drag)        | Drag item to slot        | Equips item, highlights valid slots |
| Equip (click)       | Right-click in inventory | Auto-equips to first available slot |
| Unequip             | Right-click equipped     | Returns item to inventory           |
| Compare             | Left-click in inventory  | Side-by-side tooltip comparison     |
| Sell (drag)         | Drag to sell zone        | Removes item from inventory         |
| Delete All          | Delete All button        | Clears all inventory items          |

---

## Skill System

### Skill Categories

Skills are divided into two categories with distinct purposes:

#### Main Skills (1 slot)
The hero's primary attack. Determines how the hero deals damage.

| Property                    | Description                                            |
|-----------------------------|--------------------------------------------------------|
| Required Weapon             | Hero must have this weapon type equipped to attack     |
| Damage Multiplier (%)       | Scales base physical damage                            |
| Attack Speed Multiplier (%) | Scales base attack speed                               |
| Effects                     | Special mechanics: AoE, Split, Chain, Penetration      |

#### Utility Skills (4 slots)
Support abilities that provide buffs, healing, or defensive effects.

| Property        | Description                                    |
|-----------------|------------------------------------------------|
| Sub-Category    | Recovery / Defense / Enhancement               |
| Cooldown        | Time between activations (seconds)             |
| Effect Type     | What the skill does (heal, buff, etc.)         |
| Effect Value    | Magnitude of the effect                        |

### Skill Loadout (5 slots)

```
┌──────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐
│  Main    │ │ Util 1 │ │ Util 2 │ │ Util 3 │ │ Util 4 │
│  Skill   │ │        │ │        │ │        │ │        │
│ (larger) │ │        │ │        │ │        │ │        │
└──────────┘ └────────┘ └────────┘ └────────┘ └────────┘
  Slot 0      Slot 1     Slot 2     Slot 3     Slot 4
```

- Slot 0 (Main): Only accepts Main skills
- Slots 1–4 (Utility): Only accept Utility skills
- Skills can be equipped via drag-and-drop or right-click

### Combat Gating Rules

1. **No main skill equipped → hero does not attack**
2. **Main skill requires a weapon type (e.g., Bow) → that weapon must be equipped in Main Hand**
3. **Weapon unequipped while main skill requires it → hero stops attacking immediately**
4. **Weapon re-equipped → hero resumes attacking**

### Current Skills

#### Main Skills

| Skill ID          | Name        | Weapon | Dmg Mult | Atk Spd Mult | Effects |
|-------------------|-------------|--------|----------|--------------|---------|
| basic_arrow_shot  | Arrow Shot  | Bow    | 100%     | 100%         | None    |

#### Utility Skills

| Skill ID       | Name         | Sub-Category | Cooldown | Effect         | Value |
|----------------|--------------|--------------|----------|----------------|-------|
| heal_over_time | Regeneration | Recovery     | 10s      | Heal Over Time | 5     |
| iron_skin      | Iron Skin    | Defense      | 15s      | Buff Armor     | +50   |
| wind_step      | Wind Step    | Defense      | 12s      | Buff Evasion   | +30   |
| battle_fury    | Battle Fury  | Enhancement  | 20s      | Buff Atk Speed | +25%  |
| summon_clone   | Shadow Clone | Enhancement  | 30s      | Summon Clone   | 50%   |

### Skill Effect Types

| Effect           | Category    | Description                              |
|------------------|-------------|------------------------------------------|
| AoE              | Main        | Damages all enemies in an area           |
| Split            | Main        | Projectile splits on hit                 |
| Chain             | Main        | Projectile chains to nearby enemies      |
| Penetration      | Main        | Projectile passes through enemies        |
| Heal Over Time   | Utility     | Restores health over duration            |
| Buff Armor       | Utility     | Temporarily increases armor              |
| Buff Evasion     | Utility     | Temporarily increases evasion            |
| Buff Attack Speed| Utility     | Temporarily increases attack speed       |
| Summon Clone   | Utility     | Spawns a clone that draws aggro and attacks enemies |

### Skills Tab UI

The Skills tab presents two entry points:
1. **Main** — opens main skills view with crafting placeholder + owned skills
2. **Utility** — opens utility skills organized by sub-category (Recovery / Defense / Enhancement)

Both views include an embedded loadout bar showing all 5 skill slots as drag-and-drop targets.

### Future: Skill Crafting

Main skills will be craftable (system TBD). The Skills → Main view has a reserved "Crafting (coming soon)" area for this feature.

---

## Starting Preset

New players begin with:

| Item/Skill         | Type         | Auto-Equipped | Slot      |
|--------------------|--------------|---------------|-----------|
| Basic Bow          | Item (Weapon)| Yes           | Main Hand |
| Arrow Shot         | Main Skill   | Yes           | Slot 0    |
| Regeneration       | Utility Skill| Yes           | Slot 1    |
| Iron Skin          | Utility Skill| Yes           | Slot 2    |

---

## Combat System

### Progression Hierarchy

```
Tier (Act I, Act II, ...)
 └── Map (Twilight Shore, ...)
      └── Battle (1–10 per map)
           └── Wave (2–4 per battle, invisible to player)
                └── Enemy Spawns
```

The player sees: **Tier Name** + **Battle N / Total**. Waves are internal pacing; they auto-advance once all enemies in a wave are defeated.

### Battle Flow

1. Battle starts → first wave spawns after short delay
2. Enemies march downward; hero auto-fires projectiles at nearest enemy
3. Projectiles are homing and deal damage on hit
4. When all enemies in a wave die → next wave spawns (after delay)
5. When all waves cleared → battle completes
6. Rewards granted → next battle auto-starts
7. After final battle in map → advance to next map/tier

### Enemy Archetypes

Enemies are categorized into three behavior archetypes:

| Archetype | Behavior | Attack Style |
|-----------|----------|-------------|
| Melee | Moves to target, stops at AttackRange, wind-up AoE hit | Semi-transparent red circle around attacker during wind-up, damages all targetable entities in radius |
| Ranged | Moves to AttackRange (5–8 units), fires homing projectiles | Orange enemy projectiles, slower than hero projectiles (speed = 8) |
| Caster | Stays at max range, casts spells with cast bar | Cast bar above enemy, interruptible by Silence/Stun/Knockback/Kill. Creates AoE damage zone on target position |

### Enemies

| Enemy    | HP  | Damage | Armor | Speed | Archetype | AttackRange | Appears In         |
|----------|-----|--------|-------|-------|-----------|-------------|-------------------|
| Skeleton | 30  | 5      | 2     | 2.0   | Melee     | 1.0         | Battles 0–9       |
| Zombie   | 50  | 8      | 4     | 1.2   | Melee     | 1.2         | Battles 5+ (boss) |
| Ghost    | 20  | 10     | 0     | 3.0   | Ranged    | 6.0         | Battles 7+ (adds) |

### Targeting System

Enemies select targets using aggro-weighted formula: `score = aggroWeight / distance`

| Targetable Object | Aggro Weight | Notes |
|-------------------|-------------|-------|
| Hero | 10 | Default target |
| Clone (utility skill) | 15 | Draws enemy attention away from hero |

### Ailment System

All ailments have a proc chance defined on skills and gear bonuses.

#### Elemental Ailments

| Ailment | Stacking | Effect | Duration |
|---------|----------|--------|----------|
| Ignite | 1 stack (re-applied overwrites) | DoT = 300% of triggering hit damage | 3 seconds (6 ticks × 0.5s) |
| Chill | Up to 10 stacks | -5% MoveSpeed/AttackSpeed/CastSpeed per stack. At 10 stacks: reset + Freeze (3s Stun) | Persistent while stacked |
| Shock | Up to 10 stacks | +5% damage taken per stack | Persistent while stacked |

#### Physical Ailments

| Ailment | Stacking | Effect | Duration |
|---------|----------|--------|----------|
| Bleed | Infinite (independent timers) | DoT = 60% of triggering hit damage | 5 seconds (10 ticks × 0.5s) |

#### Control Effects

| Effect | Action | Interrupts Cast? |
|--------|--------|-----------------|
| Silence | Cannot start/continue casting | Yes |
| Stun | Cannot move, attack, or cast | Yes |
| Slow | Reduces MoveSpeed by % | No |
| Knockback | Pushback + brief Stun (0.5s) | Yes |
| Freeze | = Stun for 3s (from 10 Chill stacks) | Yes |

### Scaling

- **Tier scaling:** `1.0 + tierIndex × 0.5` (multiplies enemy HP, damage, armor)
- **Wave count:** `min(2 + battleIndex/4, 4)` waves per battle
- **Enemy count:** `min(2 + battleIndex/3 + waveIndex, 8)` enemies per wave

### Rewards

| Reward Type | Formula                               | Frequency        |
|-------------|---------------------------------------|-------------------|
| Experience  | 10 + battleIndex × 5                  | Every battle      |
| Gold        | 5 + battleIndex × 2                   | Every 3rd battle  |
| Item Drop   | 30–65% chance per enemy kill          | Random per enemy  |

### Damage Model

- Hero fires projectiles at nearest enemy
- Damage = hero's Physical Damage (modified by main skill multiplier)
- Attack rate = hero's Attack Speed (modified by main skill multiplier)
- Projectiles are homing, speed = 12 units/second
- Damage numbers appear at hit position (larger + yellow for crits)
- Enemies attack hero (melee AoE, ranged projectiles, or caster spells)
- Armor reduction formula: `reduction = armor / (armor + 10 × rawDamage)`

### Visual Representation

All combat entities are rendered as instanced quads (no GameObjects):
- **Hero:** Blue quad
- **Enemy:** Red quad
- **Hero Projectile:** Yellow quad
- **Enemy Projectile:** Orange quad
- **Clone:** Green quad (slightly smaller than hero)
- **Melee AoE Zone:** Semi-transparent red circle (during wind-up)
- **Spell AoE Zone:** Semi-transparent purple circle (during cast delay)
- **HP Bars:** Green fill on dark red background (above enemy, hidden at full HP)
- **Cast Bars:** Purple fill on dark background (above casting enemy)
- **Effect Icons:** Colored squares below HP bar (orange=Ignite, blue=Chill, yellow=Shock, red=Bleed, gray=Stun, purple=Silence)

Damage numbers use pooled world-space TextMeshPro elements. All visual indicators can be toggled via Settings.

---

## UI Tabs

### Character Tab
Displays all hero stats grouped by category:
- Offense: Physical Damage, Attack Speed, Crit Chance, Crit Multiplier
- Defense: Max Health, Armor, Evasion
- Utility: Movement Speed, Health Regen

Stats update in real-time when equipment changes.

### Equipment Tab
Two-column equipment display (10 slots) + scrollable inventory grid below.
- Drag items from inventory to equipment slots
- Right-click to quick-equip/unequip
- Click to compare with currently equipped
- Sell zone for disposing items
- Item count display (current / capacity)

### Skills Tab
Category chooser (Main / Utility) → sub-view with embedded loadout bar.
- Drag skills from grid to loadout slots
- Right-click skill in grid to auto-equip
- Right-click loadout slot to unequip
- Visual skill slot highlighting during drag

---

## Settings

Accessible via Game Menu → Settings button. Toggles:
- **Show HP Bars** (default: ON) — enemy HP bars above enemies
- **Show Effect Indicators** (default: ON) — ailment/CC icons below HP bars
- **Show Damage Numbers** (default: ON) — floating damage numbers

Settings persist via PlayerPrefs.

---

## Branch Growth Progression (Main Talent System)

### Overview

Branch Growth is the main talent/progression system.  
The player grows and places procedural branches on a tree grid. Each placed node gives an immediate passive bonus.  
When enough active nodes of the same type are collected, alliance thresholds activate additional buffs.

### Core Loop

1. Collect seeds
2. Insert exactly 3 seeds into growth slot
3. Wait for branch growth timer
4. Place generated branch on tree grid
5. Node bonuses activate immediately
6. Alliance thresholds recalculate and may activate buffs
7. Level progression expands available tree area

### Seeds

- A branch is always generated from **exactly 3 seeds**
- Seed order does not matter
- Seed composition defines weighted node pool

Initial seed types:
- Fire
- Speed
- Defense
- Crit
- Bleed
- Utility
- Growth
- Universal

Each seed contributes:
- Node weighting
- Visual identity (color/theme)

### Branch Generation

Growth rules:
- Base growth time: **20s**
- Branch shape and all nodes are generated when timer completes
- Branch is placed by player after generation

Shape rules:
- Connected tile shape only (no disconnected tiles)
- Early length: **3–4 tiles**
- Slight curvature allowed
- Rotation: **90° steps before placement only**

Example early shapes:

```text
Straight
####

L-shape
###
  #

Z-shape
 ##
##
```

### Nodes

Each branch tile contains one node.

Node categories:
- **Small Node** — basic stat bonus (for example: fire damage, attack speed, armor, crit chance)
- **Special Node** (rare) — socket, hybrid, growth modifier, alliance amplifier

Socket node rules:
- Allows attaching another branch
- Low spawn chance
- Cannot appear on branch endpoints (internal tiles only)
- Does not count toward alliance thresholds

### Tree Structure

#### Trunk (start state)

- Trunk height: **4 tiles**
- First 2 trunk tiles are locked
- Remaining trunk tiles expose sockets on both sides
- Initial usable sockets: **8**

#### Grid

- Initial width: **7 tiles left + 2 trunk + 7 right = 16**
- Height grows with progression
- Vertical scrolling enabled
- Unlock area expands over time (non-rectangular/spherical growth profile)

Initial unlock example (start preset when tree unlock reaches level 2):

```text
4
6
8
10
10
10
```

### Placement and Removal Rules

Placement:
- Branch must connect to an active socket
- Branch must fit inside unlocked area
- Branches cannot overlap
- Rotation allowed only before confirmation
- After placement, branch transform is fixed

Removal:
- Costs currency
- Leaf-first removal only (from outer branches inward)
- Parent branch cannot be removed while child branches exist
- On removal, occupied cells are freed and node bonuses deactivate instantly

### Alliance System

Every active node has an alliance type (for example: Fire, Speed, Defense, Crit, Bleed).  
When active node count for a type reaches threshold, alliance buff activates.

Counting rules:
- Count all active nodes across entire tree
- Ignore socket nodes and empty cells
- Alliance progression depends on **node alliance types**, not on seed types directly

Seed-to-alliance clarification:
- Seeds influence branch generation (shape/node pool), but thresholds are counted only from resulting active nodes
- Example: `Fire + Fire + Fire` can generate a branch with 6 Fire nodes; this immediately satisfies Fire 6-threshold if all 6 nodes are active

Example (Fire):
- 3 nodes → +10% fire damage
- 6 nodes → ignite chance
- 10 nodes → fire explosions

### Level Progression Scaling

Hero level loop:
- Player gains experience from gameplay
- A visual XP bar is shown at the bottom of the screen (under hero/player skill slots)
- On level up, the tree unlock area expands and new placement slots become available
- Slot expansion follows a spherical/non-rectangular growth pattern
- Exact per-level slot config will be defined later; current baseline uses the level 2 preset above

Level progression increases:
- Grid height
- Grid width
- Trunk height
- Number of available sockets
- Branch length and shape complexity
- Frequency of hybrid/special outcomes

Level progression does not directly unlock:
- Seed types (all initial seeds are available)
- Node activation rules (nodes are always active when placed)

### UI Requirements

Tree view:
- Vertical scroll
- Visible grid bounds
- Socket highlights
- Active node markers
- Alliance progress counters

Branch placement UI:
- Ghost preview
- Invalid placement highlight (overlap/out-of-bounds/no-socket)
- Rotate button
- Grid snap

Alliance panel:
- Threshold list
- Current progress by type
- Active buffs list

### Data Model (High-Level)

- **Seed:** `type`, `weight`, `color`, `nodePool`
- **Branch:** `shape`, `nodes[]`, `seedTypes[]`, `generationLevel`
- **Node:** `type`, `value`, `allianceType`, `isSocket`
- **Tree:** `grid`, `branches[]`, `activeNodes[]`, `alliances[]`

### Future Extensions

- Seed/branch rarity
- Branch mutation and reroll
- Growth speed modifiers
- Negative/corrupted nodes
- Branch upgrading and BIG nodes
- Hybrid alliances
- Fruit harvesting and seasonal growth

---

## Future Systems (Planned)

- **Skill crafting** — create and upgrade main skills
- **Skill persistence** — save/load skill collection and loadout
- **Additional tiers/maps** — expanded progression content
- **Currency system** — gold economy for purchases
- **Experience/leveling** — hero level progression
- **Boss encounters** — special battles with unique mechanics (Ignite deals 1/3 damage to bosses)
- **Item enchanting** — upgrade existing items
- **Achievement system** — milestone rewards
- **Enemy subtypes** — Volatile (explodes on death), Aura casters (buff allies), Necromancers (revive dead)
- **Ailment visuals** — 2D sprite animations for Ignite/Chill/Shock/Bleed effects
- **Ranged kiting** — rare enemy variant that retreats when target is too close
