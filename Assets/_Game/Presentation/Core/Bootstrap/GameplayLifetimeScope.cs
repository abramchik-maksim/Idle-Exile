using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Game.Application.Ports;
using Game.Application.Combat;
using Game.Application.Inventory;
using Game.Application.Loot;
using Game.Infrastructure.ItemAffixes;
using Game.Application.Progression.TreeTalents;
using Game.Application.Skills;
using Game.Application.Stats;
using Game.Domain.DTOs.Combat;
using Game.Domain.DTOs.Inventory;
using Game.Domain.DTOs.Progression;
using Game.Domain.DTOs.Skills;
using Game.Domain.DTOs.Stats;
using Game.Domain.Skills.Crafting;
using Game.Infrastructure.Configs;
using Game.Infrastructure.Configs.Combat;
using Game.Infrastructure.Configs.Progression;
using Game.Infrastructure.Configs.Skills;
using Game.Infrastructure.Repositories;
using Game.Infrastructure.Services;
using Game.Presentation.Combat;
using Game.Presentation.Combat.Rendering;
using Game.Presentation.UI.Combat;
using Game.Presentation.UI.MainScreen;
using Game.Presentation.UI.Cheats;
using Game.Presentation.UI.GameMenu;
using Game.Presentation.UI.Presenters;
using Game.Presentation.UI.Services;
using Game.Presentation.UI.Settings;

namespace Game.Presentation.Core.Bootstrap
{
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField] private ItemDatabaseSO _itemDatabase;
        [SerializeField] private CombatDatabaseSO _combatDatabase;
        [SerializeField] private LootTableSO _lootTable;
        [SerializeField] private SkillDatabaseSO _skillDatabase;
        [SerializeField] private SkillGemDatabaseSO _skillGemDatabase;
        [SerializeField] private TreeTalentsDatabaseSO _treeTalentsDatabase;
        [SerializeField] private TreeUnlockProfileSO _treeUnlockProfile;
        [SerializeField] private ItemAffixDatabaseSO _itemAffixDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            // --- StartingPresetSO (resolved from parent CharacterDatabaseSO) ---
            builder.Register<StartingPresetSO>(c =>
            {
                var ctx = c.Resolve<GameSessionContext>();
                var db = c.Resolve<CharacterDatabaseSO>();
                if (db != null && db.characters != null)
                {
                    foreach (var entry in db.characters)
                    {
                        if (entry.heroClass == ctx.SelectedClass && entry.preset != null)
                            return entry.preset;
                    }
                    if (db.characters.Count > 0 && db.characters[0].preset != null)
                        return db.characters[0].preset;
                }
                Debug.LogWarning("[GameplayLifetimeScope] No StartingPresetSO found for selected class, returning null.");
                return null;
            }, Lifetime.Singleton);

            // --- MessagePipe ---
            var options = builder.RegisterMessagePipe();
            builder.RegisterBuildCallback(c => GlobalMessagePipe.SetProvider(c.AsServiceProvider()));

            builder.RegisterMessageBroker<EnemyKilledDTO>(options);
            builder.RegisterMessageBroker<DamageDealtDTO>(options);
            builder.RegisterMessageBroker<ItemAddedDTO>(options);
            builder.RegisterMessageBroker<ItemEquippedDTO>(options);
            builder.RegisterMessageBroker<ItemUnequippedDTO>(options);
            builder.RegisterMessageBroker<InventoryChangedDTO>(options);
            builder.RegisterMessageBroker<HeroStatsChangedDTO>(options);
            builder.RegisterMessageBroker<BattleStartedDTO>(options);
            builder.RegisterMessageBroker<BattleCompletedDTO>(options);
            builder.RegisterMessageBroker<WaveStartedDTO>(options);
            builder.RegisterMessageBroker<AllWavesClearedDTO>(options);
            builder.RegisterMessageBroker<LootDroppedDTO>(options);
            builder.RegisterMessageBroker<SkillEquippedDTO>(options);
            builder.RegisterMessageBroker<SkillUnequippedDTO>(options);
            builder.RegisterMessageBroker<SkillsChangedDTO>(options);
            builder.RegisterMessageBroker<SkillAffixAddedDTO>(options);
            builder.RegisterMessageBroker<SkillAffixRemovedDTO>(options);
            builder.RegisterMessageBroker<SkillGemUsedDTO>(options);
            builder.RegisterMessageBroker<BranchGrowthCompletedDTO>(options);
            builder.RegisterMessageBroker<BranchPlacedDTO>(options);
            builder.RegisterMessageBroker<BranchRemovedDTO>(options);
            builder.RegisterMessageBroker<TreeAlliancesChangedDTO>(options);
            builder.RegisterMessageBroker<TreeLevelChangedDTO>(options);
            builder.RegisterMessageBroker<TreeXpChangedDTO>(options);
            builder.RegisterMessageBroker<TreeTalentsChangedDTO>(options);

            // --- Infrastructure (Singletons) ---
            builder.Register<IRandomService>(c => new UnityRandomService(), Lifetime.Singleton);
            builder.Register<IConfigProvider>(
                _ => new ScriptableObjectConfigProvider(_itemDatabase), Lifetime.Singleton);
            builder.Register<IAffixConfigProvider>(
                _ => new ScriptableObjectAffixConfigProvider(_itemAffixDatabase), Lifetime.Singleton);
            builder.Register<IHeroItemClassProvider>(
                c => new HeroItemClassFromPresetProvider(c.Resolve<StartingPresetSO>()), Lifetime.Singleton);
            builder.Register<IItemAffixModifierResolver, ItemAffixModifierResolver>(Lifetime.Singleton);
            builder.Register<IModCatalogProvider>(
                _ => new ScriptableObjectModCatalogProvider(_itemAffixDatabase), Lifetime.Singleton);
            builder.Register<IItemAffixDisplayTextFormatter, ItemAffixDisplayTextFormatter>(Lifetime.Singleton);
            builder.Register<ICombatConfigProvider>(
                _ => new ScriptableObjectCombatConfigProvider(_combatDatabase, _lootTable), Lifetime.Singleton);
            builder.Register<ISkillConfigProvider>(
                _ => new ScriptableObjectSkillConfigProvider(_skillDatabase), Lifetime.Singleton);
            builder.Register<ISkillGemConfigProvider>(
                _ => new ScriptableObjectSkillGemConfigProvider(_skillGemDatabase), Lifetime.Singleton);
            builder.Register<IPlayerProgressRepository, FileProgressRepository>(Lifetime.Singleton);
            builder.Register<IInventoryRepository>(c =>
                new FileInventoryRepository(
                    c.Resolve<IConfigProvider>(),
                    c.Resolve<IItemAffixModifierResolver>(),
                    c.Resolve<ISaveSlotManager>()), Lifetime.Singleton);
            builder.Register<ITreeTalentsConfigProvider>(c =>
                new ScriptableObjectTreeTalentsConfigProvider(_treeTalentsDatabase, _treeUnlockProfile), Lifetime.Singleton);
            builder.Register<ITreeTalentsRepository, FileTreeTalentsRepository>(Lifetime.Singleton);
            builder.Register<IIconProvider, AddressableIconProvider>(Lifetime.Singleton);
            builder.Register<ITreeTalentsInputReader, TreeTalentsInputReader>(Lifetime.Singleton);
            builder.Register<LegacyPlayerPrefsMigrationService>(Lifetime.Singleton);

            // --- Services & Use Cases ---
            builder.Register<ItemRollingService>(Lifetime.Transient);
            builder.Register<CalculateHeroStatsUseCase>(Lifetime.Transient);
            builder.Register<EquipItemUseCase>(Lifetime.Transient);
            builder.Register<UnequipItemUseCase>(Lifetime.Transient);
            builder.Register<ProgressBattleUseCase>(Lifetime.Transient);
            builder.Register<GrantBattleRewardUseCase>(Lifetime.Transient);
            builder.Register<EquipSkillUseCase>(Lifetime.Transient);
            builder.Register<UnequipSkillUseCase>(Lifetime.Transient);
            builder.Register<ApplySkillGemUseCase>(Lifetime.Transient);
            builder.Register<RemoveSkillAffixUseCase>(Lifetime.Transient);
            builder.Register<SkillAffixRollingService>(Lifetime.Singleton);
            builder.Register<SkillGemInventory>(Lifetime.Singleton);
            builder.Register<InventoryCommandService>(Lifetime.Singleton);
            builder.Register<BranchGenerationService>(Lifetime.Singleton);
            builder.Register<TreeUnlockProfileService>(Lifetime.Singleton);
            builder.Register<RunBranchGrowthCycleUseCase>(Lifetime.Transient);
            builder.Register<ApplyTreeBranchOperationUseCase>(Lifetime.Transient);
            builder.Register<AdvanceTreeProgressionUseCase>(Lifetime.Transient);
            builder.Register<UtilitySkillRunner>(Lifetime.Singleton);
            builder.Register<WaveSpawner>(Lifetime.Singleton);
            builder.Register<DamageEventProcessor>(Lifetime.Singleton);

            // --- Views (MonoBehaviours from scene hierarchy) ---
            builder.RegisterComponentInHierarchy<MainScreenView>();
            builder.RegisterComponentInHierarchy<CharacterTabView>();
            builder.RegisterComponentInHierarchy<EquipmentTabView>();
            builder.RegisterComponentInHierarchy<SkillsTabView>();
            builder.RegisterComponentInHierarchy<TreeTalentsTabView>();
            builder.RegisterComponentInHierarchy<SkillSlotsView>();
            builder.RegisterComponentInHierarchy<CheatsView>();
            builder.RegisterComponentInHierarchy<GameMenuView>();
            builder.RegisterComponentInHierarchy<SettingsView>();

            // --- Combat (MonoBehaviours from scene hierarchy) ---
            builder.RegisterComponentInHierarchy<CombatBridge>().AsImplementedInterfaces().AsSelf();
            builder.RegisterComponentInHierarchy<CombatRenderer>().AsImplementedInterfaces().AsSelf();
            builder.RegisterComponentInHierarchy<DamageNumberPool>();

            // --- Presenters & Controllers (EntryPoints) ---
            builder.RegisterEntryPoint<MainScreenPresenter>();
            builder.RegisterEntryPoint<CharacterPresenter>();
            builder.RegisterEntryPoint<EquipmentPresenter>();
            builder.RegisterEntryPoint<SkillsPresenter>();
            builder.RegisterEntryPoint<TreeTalentsPresenter>();
            builder.RegisterEntryPoint<SkillSlotsPresenter>();
            builder.RegisterEntryPoint<CheatsPresenter>();
            builder.RegisterEntryPoint<SettingsPresenter>();
            builder.RegisterEntryPoint<GameMenuPresenter>();
            builder.RegisterEntryPoint<CombatPresenter>();
            builder.RegisterEntryPoint<SkillCraftingPresenter>();
            builder.RegisterEntryPoint<BattleFlowController>();

            // --- Game bootstrap ---
            builder.RegisterEntryPoint<GameInitializer>();
        }
    }
}
