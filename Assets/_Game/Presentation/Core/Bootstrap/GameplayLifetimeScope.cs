using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Game.Application.Ports;
using Game.Application.Combat;
using Game.Application.Debug;
using Game.Application.Inventory;
using Game.Application.Loot;
using Game.Application.Skills;
using Game.Application.Stats;
using Game.Domain.DTOs.Combat;
using Game.Domain.DTOs.Debug;
using Game.Domain.DTOs.Inventory;
using Game.Domain.DTOs.Skills;
using Game.Domain.DTOs.Stats;
using Game.Infrastructure.Configs;
using Game.Infrastructure.Configs.Combat;
using Game.Infrastructure.Configs.Skills;
using Game.Infrastructure.Repositories;
using Game.Infrastructure.Services;
using Game.Presentation.Combat;
using Game.Presentation.Combat.Rendering;
using Game.Presentation.UI.Combat;
using Game.Presentation.UI.MainScreen;
using Game.Presentation.UI.Cheats;
using Game.Presentation.UI.Presenters;
using Game.Presentation.UI.Services;

namespace Game.Presentation.Core.Bootstrap
{
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField] private ItemDatabaseSO _itemDatabase;
        [SerializeField] private CombatDatabaseSO _combatDatabase;
        [SerializeField] private LootTableSO _lootTable;
        [SerializeField] private SkillDatabaseSO _skillDatabase;
        [SerializeField] private StartingPresetSO _startingPreset;

        protected override void Configure(IContainerBuilder builder)
        {
            // --- MessagePipe ---
            var options = builder.RegisterMessagePipe();
            builder.RegisterBuildCallback(c => GlobalMessagePipe.SetProvider(c.AsServiceProvider()));

            builder.RegisterMessageBroker<TestMessageDTO>(options);
            builder.RegisterMessageBroker<CombatStartedDTO>(options);
            builder.RegisterMessageBroker<CombatEndedDTO>(options);
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

            // --- Infrastructure (Singletons) ---
            builder.Register<IRandomService>(c => new UnityRandomService(), Lifetime.Singleton);
            builder.Register<IConfigProvider>(
                _ => new ScriptableObjectConfigProvider(_itemDatabase), Lifetime.Singleton);
            builder.Register<ICombatConfigProvider>(
                _ => new ScriptableObjectCombatConfigProvider(_combatDatabase, _lootTable), Lifetime.Singleton);
            builder.Register<ISkillConfigProvider>(
                _ => new ScriptableObjectSkillConfigProvider(_skillDatabase), Lifetime.Singleton);
            builder.Register<IPlayerProgressRepository, PlayerPrefsProgressRepository>(Lifetime.Singleton);
            builder.Register<IInventoryRepository>(c =>
                new PlayerPrefsInventoryRepository(c.Resolve<IConfigProvider>()), Lifetime.Singleton);
            builder.Register<IIconProvider, AddressableIconProvider>(Lifetime.Singleton);
            builder.RegisterInstance(_startingPreset);

            // --- Services & Use Cases ---
            builder.Register<ItemRollingService>(Lifetime.Transient);
            builder.Register<CalculateHeroStatsUseCase>(Lifetime.Transient);
            builder.Register<EquipItemUseCase>(Lifetime.Transient);
            builder.Register<UnequipItemUseCase>(Lifetime.Transient);
            builder.Register<AddItemToInventoryUseCase>(Lifetime.Transient);
            builder.Register<GenerateLootUseCase>(Lifetime.Transient);
            builder.Register<ProgressBattleUseCase>(Lifetime.Transient);
            builder.Register<GrantBattleRewardUseCase>(Lifetime.Transient);
            builder.Register<SendTestMessageUseCase>(Lifetime.Transient);
            builder.Register<EquipSkillUseCase>(Lifetime.Transient);
            builder.Register<UnequipSkillUseCase>(Lifetime.Transient);

            // --- Views (MonoBehaviours from scene hierarchy) ---
            builder.RegisterComponentInHierarchy<MainScreenView>();
            builder.RegisterComponentInHierarchy<CharacterTabView>();
            builder.RegisterComponentInHierarchy<EquipmentTabView>();
            builder.RegisterComponentInHierarchy<SkillsTabView>();
            builder.RegisterComponentInHierarchy<SkillSlotsView>();
            builder.RegisterComponentInHierarchy<CheatsView>();

            // --- Combat (MonoBehaviours from scene hierarchy) ---
            builder.RegisterComponentInHierarchy<CombatBridge>();
            builder.RegisterComponentInHierarchy<CombatRenderer>();
            builder.RegisterComponentInHierarchy<DamageNumberPool>();

            // --- Presenters & Controllers (EntryPoints) ---
            builder.RegisterEntryPoint<MainScreenPresenter>();
            builder.RegisterEntryPoint<CharacterPresenter>();
            builder.RegisterEntryPoint<EquipmentPresenter>();
            builder.RegisterEntryPoint<SkillsPresenter>();
            builder.RegisterEntryPoint<SkillSlotsPresenter>();
            builder.RegisterEntryPoint<CheatsPresenter>();
            builder.RegisterEntryPoint<CombatPresenter>();
            builder.RegisterEntryPoint<BattleFlowController>();

            // --- Game bootstrap ---
            builder.RegisterEntryPoint<GameInitializer>();
        }
    }
}
