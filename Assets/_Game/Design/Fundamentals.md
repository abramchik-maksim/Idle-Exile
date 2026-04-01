# Fundamentals

## Legacy Baseline (from GAME_DESIGN.md)

### Game Overview

- Genre: 2D Idle RPG.
- Platform: PC (Unity 6, URP).
- Core loop: automatic combat -> loot -> equip gear -> progress through tiers.

The player watches an auto-battling hero fight waves of enemies on the left third of the screen, while managing equipment, skills, and stats on the right two-thirds via tabbed UI panels.

### Screen Layout

```text
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

### Stat System

#### Hero Stats

| Stat | Base Value | Description |
|------|------------|-------------|
| Max Health | 100 | Maximum hit points |
| Current Health | 100 | Current hit points |
| Physical Damage | 10 | Base damage per attack |
| Attack Speed | 1.0 | Attacks per second |
| Critical Chance | 5% | Chance to deal a critical hit |
| Critical Multiplier | 150% | Damage multiplier on critical hit |
| Armor | 5 | Flat physical damage reduction |
| Evasion | 0 | Chance to dodge incoming attacks |
| Movement Speed | 1 | Hero movement speed |
| Health Regen | 1 | Health regenerated per second |

#### Modifier Types

Stats are modified by two types of modifiers:

- **Flat** - adds a flat value (for example +15 Physical Damage)
- **Increased** - percentage-based increase (for example +20% Attack Speed)

Modifiers come from equipped items (implicit + rolled) and are recalculated when equipment changes.

### UI Tabs

#### Character Tab

Displays all hero stats grouped by category:

- Offense: Physical Damage, Attack Speed, Crit Chance, Crit Multiplier
- Defense: Max Health, Armor, Evasion
- Utility: Movement Speed, Health Regen

Stats update in real-time when equipment changes.

#### Equipment Tab

Two-column equipment display (10 slots) + scrollable inventory grid below.

- Drag items from inventory to equipment slots
- Right-click to quick-equip/unequip
- Click to compare with currently equipped
- Sell zone for disposing items
- Item count display (current / capacity)

#### Skills Tab

Category chooser (Main / Utility) -> sub-view with embedded loadout bar.

- Drag skills from grid to loadout slots
- Right-click skill in grid to auto-equip
- Right-click loadout slot to unequip
- Visual skill slot highlighting during drag

### Settings

Accessible via Game Menu -> Settings button.

Toggles:

- **Show HP Bars** (default: ON) - enemy HP bars above enemies
- **Show Effect Indicators** (default: ON) - ailment/CC icons below HP bars
- **Show Damage Numbers** (default: ON) - floating damage numbers

Settings persist via PlayerPrefs.

### Future Systems (Planned)

- **Skill crafting** - create and upgrade main skills
- **Skill persistence** - save/load skill collection and loadout
- **Additional tiers/maps** - expanded progression content
- **Currency system** - gold economy for purchases
- **Experience/leveling** - hero level progression
- **Boss encounters** - special battles with unique mechanics (Ignite deals 1/3 damage to bosses)
- **Item enchanting** - upgrade existing items
- **Achievement system** - milestone rewards
- **Enemy subtypes** - Volatile (explodes on death), Aura casters (buff allies), Necromancers (revive dead)
- **Ailment visuals** - 2D sprite animations for Ignite/Chill/Shock/Bleed effects
- **Ranged kiting** - rare enemy variant that retreats when target is too close

## Documentation Contract

### Current implementation

- Use `Code-Backed` subsections in other files as source of truth for runtime behavior.
- Legacy baseline is preserved for design intent, examples, and historical context.

### Target design

- Used for future mechanics and balancing direction.
- Must not override current formulas until implemented.
