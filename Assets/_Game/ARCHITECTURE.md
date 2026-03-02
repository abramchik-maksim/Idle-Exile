# Idle Exile — Technical Architecture

## Overview

Idle Exile is a 2D idle RPG built with **Unity 6** (URP). The game features an automatic combat system on the left third of the screen and player-facing UI on the right two-thirds, organized as switchable tabs (Character stats, Equipment & Inventory, Skills).

---

## Technology Stack

| Concern            | Technology                              |
|--------------------|-----------------------------------------|
| Engine             | Unity 6 (2D URP)                        |
| DI Container       | VContainer (LifetimeScope-based)        |
| Messaging          | MessagePipe (pub/sub events via DTOs)   |
| Async              | UniTask (async/await)                   |
| Reactive           | R3 (NuGetForUnity core, OpenUPM Unity)  |
| ECS                | Unity Entities 1.x (combat simulation)  |
| UI                 | UI Toolkit (UXML/USS), NOT uGUI         |
| Assets             | Addressables (icons, future content)     |

---

## Layer Architecture (Clean Architecture)

```
Domain  ←  Application  ←  Infrastructure
                          ←  Presentation (Core / Combat / UI)
```

### Dependency Rules

- **Domain** has zero external dependencies (no UnityEngine).
- **Application** depends only on Domain + MessagePipe. No UnityEngine (`noEngineReferences: true`).
- **Infrastructure** depends on Domain + Application + Unity.
- **Presentation.Core** depends on all layers (bootstraps the game).
- **Presentation.UI** depends on Domain + Application + Unity + Addressables.
- **Presentation.Combat** depends on Domain + Application + Unity ECS.

---

## Assembly Map

```
Game.Domain                        (noEngineReferences: true)
├── Characters, Combat, Inventory, Items, Stats, Skills
└── DTOs/ (Combat, Debug, Inventory, Skills, Stats)

Game.Application                   (noEngineReferences: true)
├── Combat, Debug, Inventory, Loot, Skills, Stats  (Use Cases)
└── Ports/  (IConfigProvider, IGameStateProvider, IRandomService, ISkillConfigProvider, etc.)

Game.Shared                        (noEngineReferences: true)
└── Extensions/

Game.Infrastructure
├── Configs/   (ItemDefinitionSO, ItemDatabaseSO, ScriptableObjectConfigProvider)
│   ├── Combat/   (ScriptableObjectCombatConfigProvider, CombatDatabaseSO, LootTableSO)
│   ├── Items/    (generated .asset files)
│   ├── Skills/   (SkillDefinitionSO, SkillDatabaseSO, ScriptableObjectSkillConfigProvider)
│   │   └── Data/ (skill .asset files)
│   └── Editor/   (ItemDatabaseCreator)
├── Repositories/  (InMemoryInventoryRepository, PlayerPrefsProgressRepository, PlayerPrefsInventoryRepository)
└── Services/  (UnityRandomService)

Game.Presentation.Core
├── Bootstrap/  (GameInitializer, GameplayLifetimeScope)
└── Editor/     (GameplaySceneSetup)

Game.Presentation.UI
├── Base/         (LayoutView – abstract MonoBehaviour base for all views)
├── MainScreen/   (MainScreenView, CharacterTabView, EquipmentTabView, SkillsTabView)
├── Combat/       (SkillSlotsView)
├── Cheats/       (CheatsView)
├── Presenters/   (MainScreenPresenter, CharacterPresenter, EquipmentPresenter, SkillsPresenter, SkillSlotsPresenter, CheatsPresenter, CombatPresenter)
├── DragDrop/     (ItemDragManipulator, SkillDragManipulator, EquipmentSlotDropZone)
├── Tooltip/      (ItemTooltip)
├── Services/     (IIconProvider, AddressableIconProvider)
├── Styles/       (Common.uss)
└── Editor/       (PanelSettingsSetup)

Game.Presentation.Combat
├── Components/  (HeroTag, EnemyTag, ProjectileTag, DeadTag, Position2D, CombatStats, AttackCooldown, AttackEnabled, ProjectileData, ActorId)
├── Systems/     (DamageEventBufferSystem, HeroAttackSystem, ProjectileMovementSystem, ProjectileHitSystem, DeathCleanupSystem)
├── Rendering/   (CombatRenderer, DamageNumber, DamageNumberPool)
└── CombatBridge (wave/battle orchestration, entity lifecycle, skill/weapon validation)

Game.Infrastructure.Configs.Editor  (Editor-only)
└── ItemDatabaseCreator
```

---

## File Counts

| Layer              | C# Files |
|--------------------|----------|
| Domain             | 51       |
| Application        | 19       |
| Shared             | 1        |
| Infrastructure     | 17       |
| Presentation.Core  | 4        |
| Presentation.UI    | 21       |
| Presentation.Combat| 23       |
| **Total**          | **138**  |

---

## Domain Layer

### Models

| Class              | Purpose                                           |
|--------------------|---------------------------------------------------|
| `HeroState`        | Player character identity and base data            |
| `EnemyState`       | Enemy identity and state                           |
| `ItemDefinition`   | Immutable item template (id, name, rarity, slot, handedness, weaponType, iconAddress, implicit modifiers) |
| `ItemInstance`     | Concrete item with rolled modifiers + unique ID    |
| `Inventory`        | Item storage (list + equipped dictionary)          |
| `StatCollection`   | Stat aggregation with modifier stacking            |
| `Modifier`         | Single stat modifier (stat, type, value, source)   |
| `DamageCalculator` | Pure damage computation                            |
| `DamageResult`     | Damage calculation output                          |
| `SkillDefinition`  | Immutable skill template (id, name, category, weapon req, multipliers, effects) |
| `SkillInstance`    | Owned skill with unique ID + level                 |
| `SkillCollection`  | All skills owned by the player                     |
| `SkillLoadout`     | 5-slot equipped skill bar (1 main + 4 utility)     |

### Enums

`StatType`, `ModifierType`, `EquipmentSlotType`, `Handedness`, `Rarity`, `DamageType`, `SkillCategory`, `UtilitySubCategory`, `WeaponType`, `SkillEffectType`

### Equipment Slot System

The equipment system uses a two-column layout with 10 slots:

| Left Column     | Right Column    |
|-----------------|-----------------|
| Helmet (Armor)  | Body (Armor)    |
| Amulet (Jewelry)| Gloves (Armor)  |
| Belt (Jewelry)  | Boots (Armor)   |
| Ring (Jewelry)  | Ring (Jewelry)  |
| Main Hand (Wpn) | Off Hand (Wpn)  |

`EquipmentSlotType` has both item-definition types (`Ring`, `MainHand`, `OffHand`) and position types (`Ring1`, `Ring2`). `Ring` items auto-resolve to `Ring1`/`Ring2` at equip time via `Inventory.ResolveTargetSlot()`.

**Handedness** (`Versatile`, `TwoHanded`, `OffHandOnly`, `None`):
- **Versatile** — weapon can be equipped in either `MainHand` or `OffHand`. Drag highlights both slots.
- **TwoHanded** — weapon occupies `MainHand` and blocks `OffHand`. The OffHand slot is visually dimmed.
- **OffHandOnly** — item can only go in `OffHand`.
- **None** — non-weapon items, slot determined by `EquipmentSlotType` alone.

### Skill System

Skills are split into two categories: **Main** (primary attack) and **Utility** (support/buffs).

**Skill Loadout** — 5 slots:
- Slot 0: Main skill (one primary attack)
- Slots 1–4: Utility skills

**Main Skills** define:
- `RequiredWeapon` — hero must have matching weapon equipped to attack
- `DamageMultiplierPercent` — scales base physical damage
- `AttackSpeedMultiplierPercent` — scales base attack speed
- `Effects` — list of `SkillEffectType` (AoE, Split, Chain, Penetration)

**Utility Skills** define:
- `Cooldown`, `EffectType`, `EffectValue`
- `UtilitySubCategory` — Recovery, Defense, Enhancement

**Combat integration**: The `AttackEnabled` ECS component is added/removed from the hero entity based on:
1. A main skill must be equipped in slot 0
2. If the main skill requires a weapon type, that weapon must be equipped in MainHand

If either condition fails, `AttackEnabled` is removed and the hero stops attacking.

### DTOs (MessagePipe Events)

| DTO                    | Feature    | Published When                          |
|------------------------|------------|-----------------------------------------|
| `BattleStartedDTO`     | Combat     | New battle begins (tier/map/battle info)|
| `BattleCompletedDTO`   | Combat     | Battle completed, rewards granted       |
| `WaveStartedDTO`       | Combat     | Wave begins within a battle             |
| `AllWavesClearedDTO`   | Combat     | All waves in a battle cleared           |
| `CombatStartedDTO`     | Combat     | Combat session begins (legacy)          |
| `CombatEndedDTO`       | Combat     | Combat session ends (legacy)            |
| `EnemyKilledDTO`       | Combat     | Enemy dies                              |
| `DamageDealtDTO`       | Combat     | Damage is dealt (includes WorldX/Y)     |
| `LootDroppedDTO`       | Combat     | Loot dropped from enemy                 |
| `ItemAddedDTO`         | Inventory  | Item added to inventory                 |
| `ItemEquippedDTO`      | Inventory  | Item equipped to slot                   |
| `ItemUnequippedDTO`    | Inventory  | Item removed from slot                  |
| `InventoryChangedDTO`  | Inventory  | Inventory contents changed              |
| `SkillEquippedDTO`     | Skills     | Skill equipped to loadout slot          |
| `SkillUnequippedDTO`   | Skills     | Skill removed from loadout slot         |
| `SkillsChangedDTO`     | Skills     | Skill collection or loadout changed     |
| `HeroStatsChangedDTO`  | Stats      | Hero stats recalculated                 |
| `TestMessageDTO`       | Debug      | Test message sent via cheats            |

---

## Application Layer

### Use Cases

Each use case is a pure C# class with a single `Execute()` method:

| Use Case                     | Purpose                                          |
|------------------------------|--------------------------------------------------|
| `EquipItemUseCase`           | Equip item from inventory → slot, recalculate stats |
| `UnequipItemUseCase`         | Unequip item from slot → inventory, recalculate stats |
| `AddItemToInventoryUseCase`  | Add item to inventory (capacity check)           |
| `CalculateHeroStatsUseCase`  | Aggregate all modifiers into final stat values   |
| `GenerateLootUseCase`        | Generate loot drops from combat                  |
| `ProgressBattleUseCase`      | Advance to next battle/map/tier                  |
| `GrantBattleRewardUseCase`   | Grant rewards after battle completion             |
| `EquipSkillUseCase`          | Equip skill to loadout slot (validates weapon req)|
| `UnequipSkillUseCase`        | Unequip skill from loadout slot                  |
| `SendTestMessageUseCase`     | Publish test debug message                       |

### Ports (Interfaces)

| Interface                   | Implemented By                       |
|-----------------------------|--------------------------------------|
| `IConfigProvider`           | `ScriptableObjectConfigProvider`     |
| `ICombatConfigProvider`     | `ScriptableObjectCombatConfigProvider`|
| `ISkillConfigProvider`      | `ScriptableObjectSkillConfigProvider`|
| `IGameStateProvider`        | `GameInitializer`                    |
| `IRandomService`            | `UnityRandomService`                 |
| `IPlayerProgressRepository` | `PlayerPrefsProgressRepository`      |
| `IInventoryRepository`      | `PlayerPrefsInventoryRepository`     |

---

## Infrastructure Layer

### Config System

Items are defined as individual `ItemDefinitionSO` ScriptableObjects, collected in an `ItemDatabaseSO` registry. Skills follow the same pattern with `SkillDefinitionSO` and `SkillDatabaseSO`.

```
ItemDefinitionSO (per item)
    ├── id, itemName, rarity, slot, handedness, weaponType
    ├── iconAddress (Addressables key)
    └── implicitModifiers: List<ModifierEntry>

SkillDefinitionSO (per skill)
    ├── id, skillName, category, subCategory
    ├── requiredWeapon, damageMultiplier, attackSpeedMultiplier
    ├── effects: List<SkillEffectType>
    └── cooldown, effectType, effectValue

StartingPresetSO
    ├── startingItems: List<StartingItem>  (item id, auto-equip, target slot)
    └── startingSkills: List<StartingSkill> (skill id, auto-equip, target slot)
```

### Repositories

- `PlayerPrefsProgressRepository` — saves/loads player progress via PlayerPrefs.
- `PlayerPrefsInventoryRepository` — saves/loads inventory via PlayerPrefs + JSON.

---

## Presentation Layer

### Bootstrap (Presentation.Core)

**GameplayLifetimeScope** (VContainer LifetimeScope):
- Registers all MessagePipe brokers (18 DTO types)
- Registers infrastructure singletons (config, repos, random, icon provider)
- Registers use cases as transient
- Registers views via `RegisterComponentInHierarchy<T>()`
- Registers presenters via `RegisterEntryPoint<T>()` (auto-calls `IStartable.Start()`)
- Has `[SerializeField]` fields for `ItemDatabaseSO`, `CombatDatabaseSO`, `LootTableSO`, `SkillDatabaseSO`, `StartingPresetSO`

**GameInitializer** (`IInitializable`, `IGameStateProvider`):
- Runs before presenters (`IInitializable.Initialize()` < `IStartable.Start()`)
- Creates `HeroState`, loads `Inventory`, `SkillCollection`, `SkillLoadout`
- Applies `StartingPresetSO` for new games (items + skills)
- Implements `IGameStateProvider` for presenter access

### UI System (Presentation.UI)

#### LayoutView (Base Class)

Abstract `MonoBehaviour` base for all UI Toolkit views:
- Holds `UIDocument` reference → exposes `Root` VisualElement
- Lifecycle: `OnEnable()` → `InitializeIfNeeded()` → `OnBind()` (query elements)
- `Show()` / `Hide()` toggle `Root.style.display`
- `Q<T>(name)` / `Q(name)` convenience queries

#### Views

| View               | UXML                    | Purpose                                    |
|--------------------|-------------------------|--------------------------------------------|
| `MainScreenView`   | `MainScreenView.uxml`  | Root layout: left combat area, right panel with tab bar |
| `CharacterTabView` | `CharacterTabView.uxml` | Hero stats display grouped by category     |
| `EquipmentTabView` | `EquipmentTabView.uxml` | Equipment slots + inventory grid           |
| `SkillsTabView`    | `SkillsTabView.uxml`   | Skill category chooser (Main/Utility) + embedded loadout bar + skill grids |
| `SkillSlotsView`   | `SkillSlotsView.uxml`  | Combat area skill slots (5 slots, always visible) |
| `CheatsView`       | `CheatsView.uxml`      | Debug buttons (test message, generate item) |

#### Presenters

Pure C# classes implementing `IStartable` + `IDisposable`:

| Presenter             | View(s)            | Responsibilities                                    |
|-----------------------|--------------------|-----------------------------------------------------|
| `MainScreenPresenter` | `MainScreenView`, tab views | Tab switching (Character/Equipment/Skills) |
| `CombatPresenter`     | `MainScreenView`   | Battle/tier label updates via BattleStartedDTO       |
| `CharacterPresenter`  | `CharacterTabView` | Binds hero stats, subscribes to stat changes         |
| `EquipmentPresenter`  | `EquipmentTabView` | Equip/unequip items, inventory rendering, drag-drop  |
| `SkillsPresenter`     | `SkillsTabView`    | Skill equip/unequip, drag-drop to loadout, category browsing |
| `SkillSlotsPresenter` | `SkillSlotsView`   | Combat slots display, right-click unequip            |
| `CheatsPresenter`     | `CheatsView`       | Debug actions, random item generation                |

#### Screen Layout

```
┌───────────────┬──────────────────────────────────────────┐
│               │  [Character]  [Equipment]  [Skills]  ←   │
│   Combat      │─────────────────────────────────────────│
│   Area        │                                          │
│   (1/3)       │       Tab Content (2/3)                  │
│               │                                          │
│   transparent │       opaque dark background             │
│   camera      │                                          │
│ ┌───────────┐ │                                          │
│ │ Skill Slots│ │                                          │
│ └───────────┘ │                                          │
└───────────────┴──────────────────────────────────────────┘
```

---

## Drag & Drop System

### Item Drag & Drop

`ItemDragManipulator` (PointerManipulator on inventory slots):
1. Pointer down → capture, record start position
2. Pointer move past threshold → create ghost element with rarity styling + icon
3. Ghost follows cursor, valid equipment slots highlight green (`equipment-slot--drop-hint`)
4. Hover over valid slot → brighter green (`equipment-slot--drop-hover`)
5. Pointer up on matching equipment slot → trigger `EquipItemUseCase`
6. Also supports drag-to-sell-zone

### Skill Drag & Drop

`SkillDragManipulator` (PointerManipulator on skill slots in SkillsTabView):
1. Pointer down → capture, record start position
2. Pointer move past threshold → create ghost with skill name + category color
3. Valid loadout slots highlight green (`skill-slot--drop-hint`)
4. Hover over valid slot → brighter highlight (`skill-slot--drop-hover`)
5. Drop on matching slot → trigger `EquipSkillUseCase`
6. Main skills can only drop on slot 0; utility skills on slots 1–4
7. Right-click on loadout slot → unequip

### Alternative Equip

- **Items**: Right-click inventory item → auto-equip to first matching slot
- **Skills**: Right-click skill in grid → auto-equip to first available matching slot
- **Unequip**: Right-click equipped item/skill → return to inventory/unequip

---

## Item Visual System

### Slot Composition

Each item slot renders as:
1. **Background** — subtle rarity color fill (`slot-bg--{rarity}`)
2. **Border** — rarity-colored border (`slot-border--{rarity}`)
3. **Glow** — outer border for Rare/Unique (`slot-glow--{rarity}`)
4. **Icon** — centered sprite loaded via Addressables

### Icon Loading Pipeline

```
ItemDefinition.IconAddress → IIconProvider.LoadIconAsync()
    → AddressableIconProvider (cached) → VisualElement.style.backgroundImage
```

### Tooltips

`ItemTooltip` creates an absolute-positioned panel on hover showing:
- Item name (rarity colored), rarity label, slot type
- All modifiers (implicit + rolled)
- Side-by-side comparison with equipped item on click

---

## Combat System (ECS)

### Progression Model

```
Tier (Act I, Act II, ...)
 └── Map (Twilight Shore, ...)
      └── Battle (1..10 per map)
           └── Wave (2..4 per battle, invisible to player)
                └── Enemy Spawns (skeleton ×3, zombie ×2, ...)
```

### ECS Architecture

**Entity Types:**

| Entity     | Components                                                        |
|------------|-------------------------------------------------------------------|
| Hero       | `HeroTag`, `Position2D`, `CombatStats`, `AttackCooldown`, `AttackEnabled`, `ActorId` |
| Enemy      | `EnemyTag`, `Position2D`, `CombatStats`, `ActorId`                |
| Projectile | `ProjectileTag`, `Position2D`, `ProjectileData`                   |

**Systems (SimulationSystemGroup order):**

```
HeroAttackSystem        → fires projectile at nearest enemy (requires AttackEnabled)
ProjectileMovementSystem → moves projectiles toward targets (homing)
ProjectileHitSystem      → detects hits, applies damage, enqueues DamageEvent
DeathCleanupSystem       → destroys dead enemy entities
DamageEventBufferSystem  → drains NativeQueue<DamageEvent> into managed list
```

### AttackEnabled Gating

The `AttackEnabled` component controls whether the hero fires projectiles. `CombatBridge` manages this component reactively:

- **Added** when: main skill is equipped AND required weapon is present (or skill has no weapon requirement)
- **Removed** when: main skill is unequipped, or required weapon is unequipped
- Subscribes to: `SkillEquippedDTO`, `SkillUnequippedDTO`, `SkillsChangedDTO`, `ItemEquippedDTO`, `ItemUnequippedDTO`

When `AttackEnabled` is present, `CombatBridge` also applies the main skill's damage and attack speed multipliers to the hero's ECS `CombatStats`.

### CombatBridge (MonoBehaviour orchestrator)

- Creates hero entity from `HeroState` stats
- Manages wave delay timers, spawns enemy entities per wave
- Polls `aliveEnemyQuery` to detect wave completion
- Publishes battle/wave DTOs via MessagePipe
- Validates skill+weapon state → adds/removes `AttackEnabled` component
- Applies main skill damage/attack speed multipliers to ECS stats
- Drains `DamageEventBufferSystem.FrameEvents` → feeds `DamageNumberPool`

### Rendering (`CombatRenderer`)

Uses `Graphics.DrawMeshInstanced` — zero GameObjects for combat entities.

### Damage Numbers (`DamageNumberPool`)

Object pool of pre-allocated world-space TextMeshPro elements.

---

## Event Flow

```
User Action / ECS System
    → Use Case (pure logic)
        → MessagePipe DTO published
            → Presenter subscribes
                → View.Render*() updates UI
```

Example — equipping a skill via drag-drop:
```
User drags skill to loadout slot
    → SkillsPresenter.HandleSkillDroppedOnSlot()
        → EquipSkillUseCase.Execute() (validates category + weapon)
            → Publish SkillEquippedDTO
            → Publish SkillsChangedDTO
                → SkillsPresenter.RefreshAll() (re-renders loadout + grids)
                → SkillSlotsPresenter.RefreshSlots() (updates combat slots)
                → CombatBridge.RefreshAttackState() (adds/removes AttackEnabled)
```

Example — unequipping a weapon that a main skill requires:
```
User right-clicks MainHand weapon
    → EquipmentPresenter.HandleUnequipSlot()
        → UnequipItemUseCase.Execute()
            → Publish ItemUnequippedDTO
                → CombatBridge.RefreshAttackState()
                    → CanHeroAttack() returns false (no matching weapon)
                    → Remove AttackEnabled → hero stops firing
```

---

## VContainer Registration Summary

```csharp
// MessagePipe brokers (18 DTOs)
RegisterMessageBroker<TDto>(options)  // for each DTO type

// Infrastructure — Singleton
IRandomService          → UnityRandomService
IConfigProvider         → ScriptableObjectConfigProvider
ICombatConfigProvider   → ScriptableObjectCombatConfigProvider
ISkillConfigProvider    → ScriptableObjectSkillConfigProvider
IPlayerProgressRepo     → PlayerPrefsProgressRepository
IInventoryRepository    → PlayerPrefsInventoryRepository
IIconProvider           → AddressableIconProvider
StartingPresetSO        → Instance

// Use Cases — Transient
CalculateHeroStatsUseCase, EquipItemUseCase, UnequipItemUseCase,
AddItemToInventoryUseCase, GenerateLootUseCase, ProgressBattleUseCase,
GrantBattleRewardUseCase, EquipSkillUseCase, UnequipSkillUseCase,
SendTestMessageUseCase, ItemRollingService

// Views — MonoBehaviours from scene
MainScreenView, CharacterTabView, EquipmentTabView, SkillsTabView,
SkillSlotsView, CheatsView

// Combat — MonoBehaviours from scene
CombatBridge, CombatRenderer, DamageNumberPool

// Presenters — EntryPoints (IStartable auto-Start)
MainScreenPresenter, CharacterPresenter, EquipmentPresenter,
SkillsPresenter, SkillSlotsPresenter, CheatsPresenter,
CombatPresenter, BattleFlowController

// Bootstrap
GameInitializer  // IInitializable runs first
```

---

## Naming Conventions

| Entity           | Pattern                       | Example                     |
|------------------|-------------------------------|-----------------------------|
| Use Case         | `VerbNounUseCase`             | `EquipSkillUseCase`         |
| DTO              | `PastTenseNounDTO`            | `SkillEquippedDTO`          |
| Interface        | `IServiceName`                | `ISkillConfigProvider`      |
| View             | `FeatureView`                 | `SkillsTabView`             |
| Presenter        | `FeaturePresenter`            | `SkillsPresenter`           |
| ScriptableObject | `FeatureSO`                   | `SkillDefinitionSO`         |
| Private field    | `_camelCase`                  | `_skillDatabase`            |
| USS class        | `kebab-case` / `BEM`          | `skill-slot--drop-hint`     |

---

## Folder Structure

```
Assets/
├── _Game/
│   ├── Domain/
│   │   ├── Characters/    (HeroState, EnemyState, PlayerProgressData)
│   │   ├── Combat/        (DamageCalculator, DamageResult, DamageType)
│   │   │   └── Progression/ (TierDefinition, MapDefinition, BattleDefinition, WaveDefinition, EnemyDefinition, RewardEntry)
│   │   ├── DTOs/
│   │   │   ├── Combat/    (BattleStartedDTO, BattleCompletedDTO, WaveStartedDTO, AllWavesClearedDTO, CombatStartedDTO, CombatEndedDTO, EnemyKilledDTO, DamageDealtDTO, LootDroppedDTO)
│   │   │   ├── Debug/     (TestMessageDTO)
│   │   │   ├── Inventory/ (ItemAddedDTO, ItemEquippedDTO, ItemUnequippedDTO, InventoryChangedDTO)
│   │   │   ├── Skills/    (SkillEquippedDTO, SkillUnequippedDTO, SkillsChangedDTO)
│   │   │   └── Stats/     (HeroStatsChangedDTO)
│   │   ├── Inventory/     (Inventory)
│   │   ├── Items/         (ItemDefinition, ItemInstance, Rarity, EquipmentSlotType, Handedness, EquipmentSlotHelper)
│   │   ├── Skills/        (SkillDefinition, SkillInstance, SkillCollection, SkillLoadout, SkillCategory, UtilitySubCategory, WeaponType, SkillEffectType)
│   │   └── Stats/         (Modifier, ModifierType, StatCollection, StatType)
│   ├── Application/
│   │   ├── Combat/        (ProgressBattleUseCase, GrantBattleRewardUseCase)
│   │   ├── Debug/         (SendTestMessageUseCase)
│   │   ├── Inventory/     (EquipItemUseCase, UnequipItemUseCase, AddItemToInventoryUseCase)
│   │   ├── Loot/          (GenerateLootUseCase, ItemRollingService)
│   │   ├── Ports/         (IConfigProvider, ICombatConfigProvider, ISkillConfigProvider, IGameStateProvider, IRandomService, IPlayerProgressRepository, IInventoryRepository)
│   │   ├── Skills/        (EquipSkillUseCase, UnequipSkillUseCase)
│   │   └── Stats/         (CalculateHeroStatsUseCase)
│   ├── Infrastructure/
│   │   ├── Configs/
│   │   │   ├── Combat/    (ScriptableObjectCombatConfigProvider, CombatDatabaseSO, LootTableSO)
│   │   │   ├── Items/     (generated .asset files)
│   │   │   ├── Skills/    (SkillDefinitionSO, SkillDatabaseSO, ScriptableObjectSkillConfigProvider)
│   │   │   │   └── Data/  (basic_arrow_shot, heal_over_time, iron_skin, wind_step, battle_fury, SkillDatabase)
│   │   │   └── Editor/    (ItemDatabaseCreator)
│   │   ├── Repositories/  (PlayerPrefsProgressRepository, PlayerPrefsInventoryRepository)
│   │   └── Services/      (UnityRandomService)
│   ├── Presentation/
│   │   ├── Core/
│   │   │   ├── Bootstrap/ (GameInitializer, GameplayLifetimeScope)
│   │   │   └── Editor/    (GameplaySceneSetup)
│   │   ├── Combat/
│   │   │   ├── Components/  (HeroTag, EnemyTag, ProjectileTag, DeadTag, Position2D, CombatStats, AttackCooldown, AttackEnabled, ProjectileData, ActorId)
│   │   │   ├── Systems/     (HeroAttackSystem, ProjectileMovementSystem, ProjectileHitSystem, DeathCleanupSystem, DamageEventBufferSystem)
│   │   │   ├── Rendering/   (CombatRenderer, DamageNumber, DamageNumberPool)
│   │   │   └── CombatBridge.cs
│   │   └── UI/
│   │       ├── Base/      (LayoutView)
│   │       ├── Cheats/    (CheatsView + UXML)
│   │       ├── Combat/    (SkillSlotsView + UXML)
│   │       ├── DragDrop/  (ItemDragManipulator, SkillDragManipulator, EquipmentSlotDropZone)
│   │       ├── MainScreen/(MainScreenView, CharacterTabView, EquipmentTabView, SkillsTabView + UXML)
│   │       ├── Presenters/(MainScreenPresenter, CharacterPresenter, EquipmentPresenter, SkillsPresenter, SkillSlotsPresenter, CheatsPresenter, CombatPresenter, BattleFlowController)
│   │       ├── Services/  (IIconProvider, AddressableIconProvider)
│   │       ├── Styles/    (Common.uss)
│   │       └── Tooltip/   (ItemTooltip)
│   └── Shared/
│       └── Extensions/    (CollectionExtensions)
├── Content/
│   └── UI/
│       └── Items/         (placeholder icon sprites, Addressable)
└── Scenes/
```

---

## Setup Instructions

### First-Time Scene Setup

1. **Menu → Idle Exile → Setup → Create Gameplay Scene** — creates all GameObjects, views, camera, combat objects
2. **Menu → Idle Exile → Create Item Database** — generates ItemDefinitionSO + ItemDatabaseSO assets
3. Assign in Inspector on `GameplayLifetimeScope`:
   - `ItemDatabase.asset` → `_itemDatabase`
   - `CombatDatabase.asset` → `_combatDatabase`
   - `LootTable.asset` → `_lootTable`
   - `SkillDatabase.asset` → `_skillDatabase`
   - `StartingPreset.asset` → `_startingPreset`
4. Mark sprites in `Content/UI/Items/` as Addressable with addresses `Icons/Items/{id}`
5. Build Addressables (Window → Asset Management → Addressables → Groups → Build)
6. Enter Play Mode

### Adding Missing Views to Existing Scene

Use **Menu → Idle Exile → Setup → Add Missing Views to Scene** to add any new UI views.
