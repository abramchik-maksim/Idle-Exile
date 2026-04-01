# Items And Skills

## Legacy Baseline (from GAME_DESIGN.md)

### Item System

#### Rarity Tiers

| Rarity  | Color     | Modifiers         | Visual                        |
|---------|-----------|-------------------|-------------------------------|
| Normal  | White     | Implicit only     | Standard border               |
| Magic   | Blue      | + 1-2 rolled      | Blue border + background tint |
| Rare    | Yellow    | + 3-4 rolled      | Yellow border + glow effect   |
| Unique  | Orange    | Fixed special set | Orange border + glow effect   |

#### Equipment Slots (10 total)

| Slot       | Type    | Notes                                    |
|------------|---------|------------------------------------------|
| Helmet     | Armor   | Head protection                          |
| Body Armor | Armor   | Chest protection                         |
| Gloves     | Armor   | Hand protection                          |
| Boots      | Armor   | Foot protection, often has Movement Speed |
| Amulet     | Jewelry | Offensive/defensive stats                |
| Belt       | Jewelry | Defensive utility                        |
| Ring x2    | Jewelry | Two ring slots for stat stacking         |
| Main Hand  | Weapon  | Primary weapon (damage source)           |
| Off Hand   | Weapon  | Secondary weapon or shield               |

#### Weapon Handedness

| Handedness | Behavior                                                   |
|------------|------------------------------------------------------------|
| Versatile  | Can equip in Main Hand or Off Hand                         |
| Two-Handed | Occupies Main Hand, blocks Off Hand slot (visually dimmed) |
| Off Hand   | Can only be placed in Off Hand slot                        |

#### Weapon Types

| Type   | Associated Main Skills |
|--------|------------------------|
| Bow    | Arrow Shot             |
| Sword  | (future)               |
| Axe    | (future)               |
| Staff  | (future)               |
| Dagger | (future)               |

#### Current Items

| Item ID       | Name         | Slot      | Rarity | Weapon Type | Handedness | Key Stats                    |
|---------------|--------------|-----------|--------|-------------|------------|------------------------------|
| basic_bow     | Basic Bow    | MainHand  | Normal | Bow         | Versatile  | +10 Phys Dmg, +0.5 Atk Spd   |
| iron_sword    | Iron Sword   | MainHand  | Normal | Sword       | Versatile  | +15 Phys Dmg                 |
| iron_saber    | Iron Saber   | MainHand  | Magic  | Sword       | Versatile  | +12 Phys Dmg, +0.3 Atk Spd   |
| super_sword   | Super Sword  | MainHand  | Rare   | Sword       | Versatile  | +25 Phys Dmg, +1.0 Atk Spd   |
| iron_helmet   | Iron Helmet  | Helmet    | Normal | -           | -          | +5 Armor                     |
| leather_vest  | Leather Vest | BodyArmor | Normal | -           | -          | +8 Armor                     |
| worn_gloves   | Worn Gloves  | Gloves    | Normal | -           | -          | +3 Armor                     |
| simple_boots  | Simple Boots | Boots     | Normal | -           | -          | +2 Armor, +0.5 Move Spd      |

#### Item Interactions

| Action       | Trigger                  | Result                              |
|--------------|--------------------------|-------------------------------------|
| Equip (drag) | Drag item to slot        | Equips item, highlights valid slots |
| Equip (click)| Right-click in inventory | Auto-equips to first available slot |
| Unequip      | Right-click equipped     | Returns item to inventory           |
| Compare      | Left-click in inventory  | Side-by-side tooltip comparison     |
| Sell (drag)  | Drag to sell zone        | Removes item from inventory         |
| Delete All   | Delete All button        | Clears all inventory items          |

### Skill System

#### Skill Categories

##### Main Skills (1 slot)

The hero's primary attack. Determines how the hero deals damage.

| Property                    | Description                                            |
|----------------------------|--------------------------------------------------------|
| Required Weapon            | Hero must have this weapon type equipped to attack     |
| Damage Multiplier (%)      | Scales base physical damage                            |
| Attack Speed Multiplier (%)| Scales base attack speed                               |
| Effects                    | Special mechanics: AoE, Split, Chain, Penetration      |

##### Utility Skills (4 slots)

Support abilities that provide buffs, healing, or defensive effects.

| Property     | Description                            |
|--------------|----------------------------------------|
| Sub-Category | Recovery / Defense / Enhancement       |
| Cooldown     | Time between activations (seconds)     |
| Effect Type  | What the skill does (heal, buff, etc.) |
| Effect Value | Magnitude of the effect                |

#### Skill Loadout (5 slots)

```text
┌──────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐
│  Main    │ │ Util 1 │ │ Util 2 │ │ Util 3 │ │ Util 4 │
│  Skill   │ │        │ │        │ │        │ │        │
│ (larger) │ │        │ │        │ │        │ │        │
└──────────┘ └────────┘ └────────┘ └────────┘ └────────┘
  Slot 0      Slot 1     Slot 2     Slot 3     Slot 4
```

- Slot 0 (Main): only accepts Main skills.
- Slots 1-4 (Utility): only accept Utility skills.
- Skills can be equipped via drag-and-drop or right-click.

#### Combat Gating Rules

1. No main skill equipped -> hero does not attack.
2. Main skill requires a weapon type (for example Bow) -> that weapon must be equipped in Main Hand.
3. Weapon unequipped while main skill requires it -> hero stops attacking immediately.
4. Weapon re-equipped -> hero resumes attacking.

#### Current Skills

##### Main Skills

| Skill ID         | Name       | Weapon | Dmg Mult | Atk Spd Mult | Effects |
|------------------|------------|--------|----------|--------------|---------|
| basic_arrow_shot | Arrow Shot | Bow    | 100%     | 100%         | None    |

##### Utility Skills

| Skill ID       | Name         | Sub-Category | Cooldown | Effect         | Value |
|----------------|--------------|--------------|----------|----------------|-------|
| heal_over_time | Regeneration | Recovery     | 10s      | Heal Over Time | 5     |
| iron_skin      | Iron Skin    | Defense      | 15s      | Buff Armor     | +50   |
| wind_step      | Wind Step    | Defense      | 12s      | Buff Evasion   | +30   |
| battle_fury    | Battle Fury  | Enhancement  | 20s      | Buff Atk Speed | +25%  |
| summon_clone   | Shadow Clone | Enhancement  | 30s      | Summon Clone   | 50%   |

#### Skill Effect Types

| Effect            | Category | Description                                              |
|-------------------|----------|----------------------------------------------------------|
| AoE               | Main     | Damages all enemies in an area                           |
| Split             | Main     | Projectile splits on hit                                 |
| Chain             | Main     | Projectile chains to nearby enemies                      |
| Penetration       | Main     | Projectile passes through enemies                        |
| Heal Over Time    | Utility  | Restores health over duration                            |
| Buff Armor        | Utility  | Temporarily increases armor                              |
| Buff Evasion      | Utility  | Temporarily increases evasion                            |
| Buff Attack Speed | Utility  | Temporarily increases attack speed                       |
| Summon Clone      | Utility  | Spawns a clone that draws aggro and attacks enemies     |

#### Skills Tab UI

The Skills tab presents two entry points:

1. **Main** -> opens main skills view with crafting placeholder + owned skills.
2. **Utility** -> opens utility skills organized by sub-category (Recovery / Defense / Enhancement).

Both views include an embedded loadout bar showing all 5 skill slots as drag-and-drop targets.

#### Future: Skill Crafting

Main skills will be craftable (system TBD). The Skills -> Main view has a reserved "Crafting (coming soon)" area for this feature.

### Starting Preset

New players begin with:

| Item/Skill    | Type          | Auto-Equipped | Slot      |
|---------------|---------------|---------------|-----------|
| Basic Bow     | Item (Weapon) | Yes           | Main Hand |
| Arrow Shot    | Main Skill    | Yes           | Slot 0    |
| Regeneration  | Utility Skill | Yes           | Slot 1    |
| Iron Skin     | Utility Skill | Yes           | Slot 2    |

## Current Implementation (Code-Backed)

- Item and skill bonuses feed combat stat pipelines through recalculation flows.
- Skill-affix pipeline exists and is active (`SkillAffixType`, `ApplySkillGemUseCase`, `SkillAffixRollingService`, `CombatBridge` mapping).
- Main-skill weapon gating and loadout restrictions are enforced in gameplay.

## Target Design

- Expand crafting and affix depth while preserving deterministic stat aggregation order.
