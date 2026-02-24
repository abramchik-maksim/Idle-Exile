using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Game.Application.Ports;
using Game.Application.Combat;
using Game.Application.Debug;
using Game.Application.Inventory;
using Game.Application.Loot;
using Game.Application.Stats;
using Game.Domain.DTOs.Combat;
using Game.Domain.DTOs.Debug;
using Game.Domain.DTOs.Inventory;
using Game.Domain.DTOs.Stats;
using Game.Infrastructure.Configs;
using Game.Infrastructure.Repositories;
using Game.Infrastructure.Services;
using Game.Presentation.UI.MainScreen;
using Game.Presentation.UI.Cheats;
using Game.Presentation.UI.Presenters;
using Game.Presentation.UI.Services;

namespace Game.Presentation.Core.Bootstrap
{
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField] private ItemDatabaseSO _itemDatabase;

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

            // --- Infrastructure (Singletons) ---
            builder.Register<IRandomService>(c => new UnityRandomService(), Lifetime.Singleton);
            builder.Register<IConfigProvider>(
                _ => new ScriptableObjectConfigProvider(_itemDatabase), Lifetime.Singleton);
            builder.Register<IPlayerProgressRepository, PlayerPrefsProgressRepository>(Lifetime.Singleton);
            builder.Register<IInventoryRepository, InMemoryInventoryRepository>(Lifetime.Singleton);
            builder.Register<IIconProvider, AddressableIconProvider>(Lifetime.Singleton);

            // --- Use Cases (Transient) ---
            builder.Register<CalculateHeroStatsUseCase>(Lifetime.Transient);
            builder.Register<EquipItemUseCase>(Lifetime.Transient);
            builder.Register<UnequipItemUseCase>(Lifetime.Transient);
            builder.Register<AddItemToInventoryUseCase>(Lifetime.Transient);
            builder.Register<GenerateLootUseCase>(Lifetime.Transient);
            builder.Register<StartCombatSessionUseCase>(Lifetime.Transient);
            builder.Register<SendTestMessageUseCase>(Lifetime.Transient);

            // --- Views (MonoBehaviours from scene hierarchy) ---
            builder.RegisterComponentInHierarchy<MainScreenView>();
            builder.RegisterComponentInHierarchy<CharacterTabView>();
            builder.RegisterComponentInHierarchy<EquipmentTabView>();
            builder.RegisterComponentInHierarchy<CheatsView>();

            // --- Presenters as EntryPoints (IStartable → auto-calls Start()) ---
            builder.RegisterEntryPoint<MainScreenPresenter>();
            builder.RegisterEntryPoint<CharacterPresenter>();
            builder.RegisterEntryPoint<EquipmentPresenter>();
            builder.RegisterEntryPoint<CheatsPresenter>();

            // --- Game bootstrap ---
            builder.RegisterEntryPoint<GameInitializer>();
        }
    }
}
