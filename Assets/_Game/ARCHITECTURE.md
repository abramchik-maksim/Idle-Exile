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
└── (ECS Components, Systems – future)

Game.Infrastructure.Configs.Editor  (Editor-only)
└── ItemDatabaseCreator
```

---

## File Counts

| Layer              | C# Files |
|--------------------|----------|
| Domain             | 23       |
| Application        | 12       |
| Shared             | 1        |
| Infrastructure     | 9        |
| Presentation.Core  | 3        |
| Presentation.UI    | 20       |
| **Total**          | **68**   |

---

## Domain Layer

### Models

| Class              | Purpose                                           |
|--------------------|---------------------------------------------------|
| `HeroState`        | Player character identity and base data            |
| `EnemyState`       | Enemy identity and state                           |
| `ItemDefinition`   | Immutable item template (id, name, rarity, slot, iconAddress, implicit modifiers) |
| `ItemInstance`     | Concrete item with rolled modifiers + unique ID    |
| `Inventory`        | Item storage (list + equipped dictionary)          |
| `StatCollection`   | Stat aggregation with modifier stacking            |
| `Modifier`         | Single stat modifier (stat, type, value, source)   |
| `DamageCalculator` | Pure damage computation                            |
| `DamageResult`     | Damage calculation output                          |

### Enums

`StatType`, `ModifierType`, `EquipmentSlotType`, `Rarity`, `DamageType`

### DTOs (MessagePipe Events)

| DTO                    | Feature    | Published When                          |
|------------------------|------------|-----------------------------------------|
| `CombatStartedDTO`     | Combat     | Combat session begins                   |
| `CombatEndedDTO`       | Combat     | Combat session ends                     |
| `EnemyKilledDTO`       | Combat     | Enemy dies                              |
| `DamageDealtDTO`       | Combat     | Damage is dealt                         |
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
| `SendTestMessageUseCase`     | Publish test debug message                       |

### Ports (Interfaces)

| Interface                   | Implemented By                    |
|-----------------------------|-----------------------------------|
| `IConfigProvider`           | `ScriptableObjectConfigProvider`  |
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
    ├── id, itemName, rarity, slot
    ├── iconAddress (Addressables key)
    └── implicitModifiers: List<ModifierEntry>

ItemDatabaseSO (registry)
    └── items: List<ItemDefinitionSO>

ScriptableObjectConfigProvider : IConfigProvider
    └── Reads ItemDatabaseSO → Dictionary<string, ItemDefinition>
```

The editor menu item **Idle Exile → Create Item Database** auto-generates all 6 initial items.

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
| `MainScreenPresenter`| `MainScreenView`, tab views | Tab switching, combat HUD updates           |
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

Equipment slots support click-to-unequip via `UnequipItemUseCase`.

### Tooltips

`ItemTooltip` static class creates an absolute-positioned panel on hover showing:
- Item name (rarity colored)
- Rarity label (if not Normal)
- Slot type
- Separator
- All modifiers (implicit + rolled)

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
// MessagePipe brokers (10 DTOs)
RegisterMessageBroker<TDto>(options)  // for each DTO type

// Infrastructure — Singleton
IRandomService        → UnityRandomService
IConfigProvider       → ScriptableObjectConfigProvider(itemDatabase)
IPlayerProgressRepo   → PlayerPrefsProgressRepository
IInventoryRepository  → InMemoryInventoryRepository
IIconProvider         → AddressableIconProvider

// Use Cases — Transient
CalculateHeroStatsUseCase, EquipItemUseCase, UnequipItemUseCase,
AddItemToInventoryUseCase, GenerateLootUseCase, StartCombatSessionUseCase,
SendTestMessageUseCase

// Views — MonoBehaviours from scene
RegisterComponentInHierarchy<MainScreenView>()
RegisterComponentInHierarchy<CharacterTabView>()
RegisterComponentInHierarchy<EquipmentTabView>()
RegisterComponentInHierarchy<CheatsView>()

// Presenters — EntryPoints (IStartable auto-Start)
RegisterEntryPoint<MainScreenPresenter>()
RegisterEntryPoint<CharacterPresenter>()
RegisterEntryPoint<EquipmentPresenter>()
RegisterEntryPoint<CheatsPresenter>()

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
│   │   ├── DTOs/          (Combat/, Debug/, Inventory/, Stats/)
│   │   ├── Inventory/     (Inventory)
│   │   ├── Items/         (ItemDefinition, ItemInstance, Rarity, EquipmentSlotType)
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
│   │   │   ├── Editor/    (ItemDatabaseCreator)
│   │   │   └── Items/     (generated .asset files)
│   │   ├── Repositories/  (InMemoryInventoryRepository, PlayerPrefsProgressRepository)
│   │   └── Services/      (UnityRandomService)
│   ├── Presentation/
│   │   ├── Core/
│   │   │   ├── Bootstrap/ (GameInitializer, GameplayLifetimeScope)
│   │   │   └── Editor/    (GameplaySceneSetup)
│   │   ├── Combat/        (ECS — future)
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

1. **Menu → Idle Exile → Setup Gameplay Scene** — creates all GameObjects, views, camera
2. **Menu → Idle Exile → Create Item Database** — generates ItemDefinitionSO + ItemDatabaseSO assets
3. Assign `ItemDatabase.asset` to `GameplayLifetimeScope._itemDatabase` in the Inspector
4. Mark sprites in `Content/UI/Items/` as Addressable with addresses `Icons/Items/{id}`
5. Build Addressables (Window → Asset Management → Addressables → Groups → Build)
6. Enter Play Mode
