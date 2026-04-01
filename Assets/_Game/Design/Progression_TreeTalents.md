# Progression - Tree Talents

## Legacy Baseline (from GAME_DESIGN.md)

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
- Early length: **3-4 tiles**
- Slight curvature allowed
- Rotation: **90-degree steps before placement only**

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

- **Small Node** - basic stat bonus (for example: fire damage, attack speed, armor, crit chance)
- **Special Node** (rare) - socket, hybrid, growth modifier, alliance amplifier

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

- 3 nodes -> +10% fire damage
- 6 nodes -> ignite chance
- 10 nodes -> fire explosions

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

## Current Implementation (Code-Backed)

### Sources

- `Assets/_Game/Domain/Progression/TreeTalents/TreeTalentsState.cs`
- `Assets/_Game/Application/Progression/TreeTalents/BranchGenerationService.cs`

### Runtime Notes

- Growth requires exactly 3 seeds in runtime use case.
- Placement validation checks socket anchor, bounds profile, overlap and rotation transforms.
- Removal is dependency-gated by child branch anchors.
- Alliances are recalculated from active non-socket nodes.
- Tree XP progression is formula-driven in state (`100 + (level - 1) * 25` for next level threshold).

## Target Design

- Connect alliance/node effects directly into hero combat stat pipelines.
- Add explicit gameplay effects per threshold where currently represented only as counters/UI.
