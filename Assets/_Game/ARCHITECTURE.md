# Idle Exile — Technical Architecture

## Overview

Idle Exile is a 2D idle RPG built with **Unity 6** (URP). The game features an automatic combat system on the left third of the screen and player-facing UI on the right two-thirds, organized as switchable tabs (Character stats, Equipment & Inventory).

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
| Assets             | Addressables (icons, future content)    |

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
├── Characters, Combat, Inventory, Items, Stats
└── DTOs/ (Combat, Debug, Inventory, Stats)

Game.Application                   (noEngineReferences: true)
├── Combat, Debug, Inventory, Loot, Stats  (Use Cases)
└── Ports/  (IConfigProvider, IGameStateProvider, IRandomService, etc.)

Game.Shared                        (noEngineReferences: true)
└── Extensions/

Game.Infrastructure
├── Configs/   (ItemDefinitionSO, ItemDatabaseSO, ScriptableObjectConfigProvider, HardcodedConfigProvider)
├── Repositories/  (InMemoryInventoryRepository, PlayerPrefsProgressRepository)
└── Services/  (UnityRandomService)

Game.Presentation.Core
├── Bootstrap/  (GameInitializer, GameplayLifetimeScope)
└── Editor/     (GameplaySceneSetup)

Game.Presentation.UI
├── Base/         (LayoutView – abstract MonoBehaviour base for all views)
├── MainScreen/   (MainScreenView, CharacterTabView, EquipmentTabView)
├── Cheats/       (CheatsView)
├── Presenters/   (MainScreenPresenter, CharacterPresenter, EquipmentPresenter, CheatsPresenter)
├── DragDrop/     (ItemDragManipulator, EquipmentSlotDropZone)
├── Tooltip/      (ItemTooltip)
├── Services/     (IIconProvider, AddressableIconProvider)
├── Styles/       (Common.uss)
└── Editor/       (PanelSettingsSetup)

Game.Presentation.Combat
├── Components/  (HeroTag, EnemyTag, ProjectileTag, DeadTag, Position2D, CombatStats, AttackCooldown, ProjectileData, ActorId)
├── Systems/     (DamageEventBufferSystem, HeroAttackSystem, ProjectileMovementSystem, ProjectileHitSystem, DeathCleanupSystem)
├── Rendering/   (CombatRenderer, DamageNumber, DamageNumberPool)
└── CombatBridge (wave/battle orchestration, entity lifecycle)

Game.Infrastructure.Configs.Editor  (Editor-only)
└── ItemDatabaseCreator
```

---

## File Counts

| Layer              | C# Files |
|--------------------|----------|
| Domain             | 31       |
| Application        | 14       |
| Shared             | 1        |
| Infrastructure     | 10       |
| Presentation.Core  | 3        |
| Presentation.UI    | 21       |
| Presentation.Combat| 16       |
| **Total**          | **96**   |

---

## Domain Layer

### Models

| Class              | Purpose                                           |
|--------------------|---------------------------------------------------|
| `HeroState`        | Player character identity and base data            |
| `EnemyState`       | Enemy identity and state                           |
| `ItemDefinition`   | Immutable item template (id, name, rarity, slot, handedness, iconAddress, implicit modifiers) |
| `ItemInstance`     | Concrete item with rolled modifiers + unique ID    |
| `Inventory`        | Item storage (list + equipped dictionary)          |
| `StatCollection`   | Stat aggregation with modifier stacking            |
| `Modifier`         | Single stat modifier (stat, type, value, source)   |
| `DamageCalculator` | Pure damage computation                            |
| `DamageResult`     | Damage calculation output                          |

### Enums

`StatType`, `ModifierType`, `EquipmentSlotType`, `Handedness`, `Rarity`, `DamageType`

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
- **Versatile** — weapon can be equipped in either `MainHand` or `OffHand`. Drag highlights both slots. RMB auto-equips to first free slot (MainHand priority). Drag-drop respects the specific target slot.
- **TwoHanded** — weapon occupies `MainHand` and blocks `OffHand`. Equipping auto-unequips the off-hand item. The OffHand slot is visually dimmed with `equipment-slot--blocked` class.
- **OffHandOnly** — item (shields, special daggers, scepters) can only go in `OffHand`.
- **None** — non-weapon items (armor, jewelry), slot is determined by `EquipmentSlotType` alone.

`EquipmentSlotHelper.IsSlotMatch(itemSlot, targetSlot, handedness)` handles matching: `Ring` → `Ring1`/`Ring2`, `Versatile` → both `MainHand`/`OffHand`.

`Inventory.TryEquip` supports an optional `targetSlotOverride` parameter for drag-drop targeting a specific slot (e.g., dragging a Versatile weapon to OffHand). When no override is provided, `ResolveTargetSlot()` auto-routes to the first available slot.

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
| `ItemAddedDTO`         | Inventory  | Item added to inventory                 |
| `ItemEquippedDTO`      | Inventory  | Item equipped to slot                   |
| `ItemUnequippedDTO`    | Inventory  | Item removed from slot                  |
| `InventoryChangedDTO`  | Inventory  | Inventory contents changed              |
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
| `StartCombatSessionUseCase`  | Initialize a combat session                      |
| `ProgressBattleUseCase`      | Advance to next battle/map/tier                  |
| `SendTestMessageUseCase`     | Publish test debug message                       |

### Ports (Interfaces)

| Interface                   | Implemented By                    |
|-----------------------------|-----------------------------------|
| `IConfigProvider`           | `ScriptableObjectConfigProvider`  |
| `ICombatConfigProvider`     | `HardcodedCombatConfigProvider`   |
| `IGameStateProvider`        | `GameInitializer`                 |
| `IRandomService`            | `UnityRandomService`              |
| `IPlayerProgressRepository` | `PlayerPrefsProgressRepository`   |
| `IInventoryRepository`      | `InMemoryInventoryRepository`     |

---

## Infrastructure Layer

### Config System

Items are defined as individual `ItemDefinitionSO` ScriptableObjects, collected in an `ItemDatabaseSO` registry. `ScriptableObjectConfigProvider` converts them to domain `ItemDefinition` objects at startup.

```
ItemDefinitionSO (per item, CreateAssetMenu)
    ├── id, itemName, rarity, slot, handedness
    ├── iconAddress (Addressables key)
    └── implicitModifiers: List<ModifierEntry>

ItemDatabaseSO (registry)
    └── items: List<ItemDefinitionSO>

ScriptableObjectConfigProvider : IConfigProvider
    └── Reads ItemDatabaseSO → Dictionary<string, ItemDefinition>
```

The editor menu item **Idle Exile → Create Item Database** auto-generates all 11 initial items (weapons, armor, jewelry). Running it again force-updates existing assets with the latest blueprint data.

### Repositories

- `PlayerPrefsProgressRepository` — saves/loads player progress via PlayerPrefs.
- `InMemoryInventoryRepository` — in-memory inventory (no persistence yet).

---

## Presentation Layer

### Bootstrap (Presentation.Core)

**GameplayLifetimeScope** (VContainer LifetimeScope):
- Registers all MessagePipe brokers
- Registers infrastructure singletons (config, repos, random, icon provider)
- Registers use cases as transient
- Registers views via `RegisterComponentInHierarchy<T>()`
- Registers presenters via `RegisterEntryPoint<T>()` (auto-calls `IStartable.Start()`)
- Has `[SerializeField] ItemDatabaseSO` for the SO config provider

**GameInitializer** (`IInitializable`, `IGameStateProvider`):
- Runs before presenters (`IInitializable.Initialize()` < `IStartable.Start()`)
- Creates `HeroState`, loads `Inventory`, sets up camera viewport
- Implements `IGameStateProvider` for presenter access

**GameplaySceneSetup** (Editor tool):
- Menu item to auto-create the gameplay scene hierarchy
- Creates camera, all view GameObjects with UIDocument + LayoutView components
- Sets panel settings, sort orders, visibility defaults

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
| `CheatsView`       | `CheatsView.uxml`      | Debug buttons (test message, generate item) |

#### Presenters

Pure C# classes implementing `IStartable` + `IDisposable`:

| Presenter            | View(s)            | Responsibilities                                    |
|----------------------|--------------------|-----------------------------------------------------|
| `MainScreenPresenter`| `MainScreenView`, tab views | Tab switching                                |
| `CombatPresenter`    | `MainScreenView`   | Battle/tier label updates via BattleStartedDTO       |
| `CharacterPresenter` | `CharacterTabView` | Binds hero stats, subscribes to stat changes         |
| `EquipmentPresenter` | `EquipmentTabView` | Equip/unequip, inventory rendering, icon provider    |
| `CheatsPresenter`    | `CheatsView`       | Debug actions, random item generation                |

#### Screen Layout

```
┌───────────────┬──────────────────────────────────────────┐
│               │  [Character]  [Equipment]    ← tab bar  │
│   Combat      │─────────────────────────────────────────│
│   Area        │                                          │
│   (1/3)       │       Tab Content (2/3)                  │
│               │                                          │
│   transparent │       opaque dark background             │
│   camera      │                                          │
└───────────────┴──────────────────────────────────────────┘
```

Each tab content view is a separate `UIDocument` with higher sort order, positioned below the tab bar (`top: 52px`).

---

## Item Visual System

### Slot Composition

Each item slot (inventory or equipment) renders as:

1. **Background** — subtle rarity color fill (`slot-bg--{rarity}`)
2. **Border** — rarity-colored border (`slot-border--{rarity}`), thicker for Rare/Unique
3. **Glow** — outer border element for Rare/Unique (`slot-glow--{rarity}`)
4. **Icon** — centered sprite loaded via Addressables, with colored placeholder fallback

No text labels in slots — item info appears only in hover tooltips.

### Icon Loading Pipeline

```
ItemDefinition.IconAddress (string, e.g. "Icons/Items/rusty_sword")
    → IIconProvider.LoadIconAsync(address)
        → AddressableIconProvider (cached Dictionary<string, Sprite>)
            → VisualElement.style.backgroundImage = sprite
```

Placeholder colored squares display while sprites load or if no address is set.

### Drag & Drop

`ItemDragManipulator` (PointerManipulator on inventory slots):
1. Pointer down → capture, record start position
2. Pointer move past threshold → create ghost element with rarity styling + icon
3. Ghost follows cursor, valid drop targets highlight green
4. Pointer up on matching equipment slot → trigger `EquipItemUseCase`

Equipment slots support RMB-to-unequip and drag-to-unequip via `UnequipItemUseCase`. LMB click on an inventory item shows a side-by-side comparison tooltip with the currently equipped item.

### Tooltips

`ItemTooltip` static class creates an absolute-positioned panel on hover showing:
- Item name (rarity colored)
- Rarity label (if not Normal)
- Slot type
- Separator
- All modifiers (implicit + rolled)

---

## Combat System (ECS)

### Progression Model

The game uses a hierarchical progression structure:

```
Tier (Act I, Act II, ...)
 └── Map (Twilight Shore, ...)
      └── Battle (1..10 per map)
           └── Wave (2..4 per battle, invisible to player)
                └── Enemy Spawns (skeleton ×3, zombie ×2, ...)
```

Player sees: **Tier name** + **Battle N / Total**. Waves are internal and auto-advance.

After a battle completes: rewards are granted, `ProgressBattleUseCase` advances progress, next battle auto-starts.

### Domain Models (`Domain/Combat/Progression/`)

| Class               | Purpose                                                    |
|---------------------|------------------------------------------------------------|
| `TierDefinition`    | Tier identity, ordered, references maps                    |
| `MapDefinition`     | Map identity, references battles                           |
| `BattleDefinition`  | Battle identity, ordered, contains waves + rewards         |
| `WaveDefinition`    | Spawn entries + delay before wave                          |
| `WaveSpawnEntry`    | Enemy definition ID + count                                |
| `EnemyDefinition`   | Enemy template: base HP, damage, armor, speed              |
| `RewardEntry`       | Reward type (Item/Currency/XP) + amount                    |

### Config Provider

`ICombatConfigProvider` — port for progression data:
- `GetTier/Map/Battle(index)`, `GetEnemy(id)`, `GetTierScaling(tierIndex)`
- Implemented by `HardcodedCombatConfigProvider` (procedurally generated Tier 1 / 1 map / 10 battles)

### ECS Architecture

All combat simulation runs in Unity ECS (Entities 1.x). Logic is in `Game.Presentation.Combat`.

**Entity Types:**

| Entity     | Components                                            |
|------------|-------------------------------------------------------|
| Hero       | `HeroTag`, `Position2D`, `CombatStats`, `AttackCooldown`, `ActorId` |
| Enemy      | `EnemyTag`, `Position2D`, `CombatStats`, `ActorId`    |
| Projectile | `ProjectileTag`, `Position2D`, `ProjectileData`       |

**Systems (SimulationSystemGroup order):**

```
HeroAttackSystem        → fires projectile at nearest enemy
ProjectileMovementSystem → moves projectiles toward targets (homing)
ProjectileHitSystem      → detects hits, applies damage, enqueues DamageEvent
DeathCleanupSystem       → destroys dead enemy entities
DamageEventBufferSystem  → drains NativeQueue<DamageEvent> into managed list
```

### CombatBridge (MonoBehaviour orchestrator)

Manages battle/wave lifecycle from managed code:
- Creates hero entity once from `HeroState` stats
- Manages wave delay timers, spawns enemy entities per wave
- Polls `aliveEnemyQuery` to detect wave completion
- Publishes `BattleStartedDTO`, `BattleCompletedDTO`, `WaveStartedDTO` via MessagePipe
- Drains `DamageEventBufferSystem.FrameEvents` in `LateUpdate()` → feeds `DamageNumberPool`
- Auto-advances battles via `ProgressBattleUseCase`

### Rendering (`CombatRenderer`)

Uses `Graphics.DrawMeshInstanced` — zero GameObjects for combat entities:
- Queries ECS for entity positions by tag (`HeroTag`, `EnemyTag`, `ProjectileTag`)
- Builds `Matrix4x4[]` arrays per entity type
- Draws all entities of each type in a single instanced draw call
- Hero = blue quad, Enemy = red quad, Projectile = yellow quad
- Auto-creates materials with URP/Unlit shader if none assigned

### Damage Numbers (`DamageNumberPool`)

Object pool of 50 pre-allocated `DamageNumber` GameObjects (world-space `TextMeshPro`):
- On hit: position near enemy, display rounded damage, animate float-up + fade-out
- Critical hits: larger font, yellow color
- Pool exhaustion: recycles oldest active number
- Duration: 0.8s per number

### Damage Event Flow (NativeQueue batching)

```
ProjectileHitSystem (ECS, per frame)
  → NativeQueue<DamageEvent>.Enqueue(amount, position, isCrit)
      → DamageEventBufferSystem.OnUpdate() drains queue → FrameEvents list
          → CombatBridge.LateUpdate() reads FrameEvents
              → DamageNumberPool.Show(position, amount, isCrit)
              → Publish DamageDealtDTO via MessagePipe
```

### Performance TODOs

- **DamageCalculator in Burst**: Currently uses managed `DamageCalculator` (option A). Domain formula is the source of truth. Will duplicate as ECS-native calculation when formulas stabilize.
- **Spatial partitioning**: Collision detection is O(projectiles × enemies). Implement spatial hash when entity counts exceed ~200.

---

## Event Flow

```
User Action / ECS System
    → Use Case (pure logic)
        → MessagePipe DTO published
            → Presenter subscribes
                → View.Render*() updates UI
```

Example — equipping an item:
```
User drags item to slot
    → EquipmentPresenter.HandleItemDroppedOnSlot()
        → EquipItemUseCase.Execute() (moves item, recalculates stats)
            → Publish ItemEquippedDTO
            → Publish InventoryChangedDTO
            → Publish HeroStatsChangedDTO
                → EquipmentPresenter.RefreshAll() (re-renders slots + grid)
                → CharacterPresenter (updates stat display)
```

---

## VContainer Registration Summary

```csharp
// MessagePipe brokers (14 DTOs)
RegisterMessageBroker<TDto>(options)  // for each DTO type

// Infrastructure — Singleton
IRandomService          → UnityRandomService
IConfigProvider         → ScriptableObjectConfigProvider(itemDatabase)
ICombatConfigProvider   → HardcodedCombatConfigProvider
IPlayerProgressRepo     → PlayerPrefsProgressRepository
IInventoryRepository    → InMemoryInventoryRepository
IIconProvider           → AddressableIconProvider

// Use Cases — Transient
CalculateHeroStatsUseCase, EquipItemUseCase, UnequipItemUseCase,
AddItemToInventoryUseCase, GenerateLootUseCase, StartCombatSessionUseCase,
ProgressBattleUseCase, SendTestMessageUseCase

// Views — MonoBehaviours from scene
RegisterComponentInHierarchy<MainScreenView>()
RegisterComponentInHierarchy<CharacterTabView>()
RegisterComponentInHierarchy<EquipmentTabView>()
RegisterComponentInHierarchy<CheatsView>()

// Combat — MonoBehaviours from scene
RegisterComponentInHierarchy<CombatBridge>()
RegisterComponentInHierarchy<CombatRenderer>()
RegisterComponentInHierarchy<DamageNumberPool>()

// Presenters — EntryPoints (IStartable auto-Start)
RegisterEntryPoint<MainScreenPresenter>()
RegisterEntryPoint<CharacterPresenter>()
RegisterEntryPoint<EquipmentPresenter>()
RegisterEntryPoint<CheatsPresenter>()
RegisterEntryPoint<CombatPresenter>()

// Bootstrap
RegisterEntryPoint<GameInitializer>()  // IInitializable runs first
```

---

## Naming Conventions

| Entity           | Pattern                       | Example                     |
|------------------|-------------------------------|-----------------------------|
| Use Case         | `VerbNounUseCase`             | `EquipItemUseCase`          |
| DTO              | `PastTenseNounDTO`            | `ItemEquippedDTO`           |
| Interface        | `IServiceName`                | `IConfigProvider`           |
| View             | `FeatureView`                 | `EquipmentTabView`          |
| Presenter        | `FeaturePresenter`            | `EquipmentPresenter`        |
| ScriptableObject | `FeatureSO`                   | `ItemDefinitionSO`          |
| Private field    | `_camelCase`                  | `_iconProvider`             |
| USS class        | `kebab-case` / `BEM`          | `slot-bg--magic`            |

---

## Folder Structure

```
Assets/
├── _Game/
│   ├── Domain/
│   │   ├── Characters/    (HeroState, EnemyState)
│   │   ├── Combat/        (DamageCalculator, DamageResult, DamageType)
│   │   │   └── Progression/ (TierDefinition, MapDefinition, BattleDefinition, WaveDefinition, EnemyDefinition, RewardEntry, ...)
│   │   ├── DTOs/          (Combat/, Debug/, Inventory/, Stats/)
│   │   ├── Inventory/     (Inventory)
│   │   ├── Items/         (ItemDefinition, ItemInstance, Rarity, EquipmentSlotType, Handedness, EquipmentSlotHelper)
│   │   └── Stats/         (Modifier, ModifierType, StatCollection, StatType)
│   ├── Application/
│   │   ├── Combat/        (StartCombatSessionUseCase)
│   │   ├── Debug/         (SendTestMessageUseCase)
│   │   ├── Inventory/     (EquipItemUseCase, UnequipItemUseCase, AddItemToInventoryUseCase)
│   │   ├── Loot/          (GenerateLootUseCase)
│   │   ├── Ports/         (IConfigProvider, IGameStateProvider, IRandomService, ...)
│   │   └── Stats/         (CalculateHeroStatsUseCase)
│   ├── Infrastructure/
│   │   ├── Configs/       (ItemDefinitionSO, ItemDatabaseSO, ScriptableObjectConfigProvider)
│   │   │   ├── Combat/    (HardcodedCombatConfigProvider)
│   │   │   ├── Editor/    (ItemDatabaseCreator)
│   │   │   └── Items/     (generated .asset files)
│   │   ├── Repositories/  (InMemoryInventoryRepository, PlayerPrefsProgressRepository)
│   │   └── Services/      (UnityRandomService)
│   ├── Presentation/
│   │   ├── Core/
│   │   │   ├── Bootstrap/ (GameInitializer, GameplayLifetimeScope)
│   │   │   └── Editor/    (GameplaySceneSetup)
│   │   ├── Combat/
│   │   │   ├── Components/  (HeroTag, EnemyTag, ProjectileTag, Position2D, CombatStats, ...)
│   │   │   ├── Systems/     (HeroAttackSystem, ProjectileMovementSystem, ProjectileHitSystem, ...)
│   │   │   ├── Rendering/   (CombatRenderer, DamageNumber, DamageNumberPool)
│   │   │   └── CombatBridge.cs
│   │   └── UI/
│   │       ├── Base/      (LayoutView)
│   │       ├── Cheats/    (CheatsView + UXML)
│   │       ├── DragDrop/  (ItemDragManipulator, EquipmentSlotDropZone)
│   │       ├── MainScreen/(MainScreenView, CharacterTabView, EquipmentTabView + UXML)
│   │       ├── Presenters/(all presenters)
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
3. Assign `ItemDatabase.asset` to `GameplayLifetimeScope._itemDatabase` in the Inspector
4. Mark sprites in `Content/UI/Items/` as Addressable with addresses `Icons/Items/{id}`
5. Build Addressables (Window → Asset Management → Addressables → Groups → Build)
6. Enter Play Mode

### Adding Combat to Existing Scene

If the scene was created before the combat system, use **Menu → Idle Exile → Setup → Add Missing Views to Scene**. Additionally, manually add a `[Combat]` parent GameObject with children:
- `CombatBridge` (add `CombatBridge` component)
- `CombatRenderer` (add `CombatRenderer` component)
- `DamageNumberPool` (add `DamageNumberPool` component)
