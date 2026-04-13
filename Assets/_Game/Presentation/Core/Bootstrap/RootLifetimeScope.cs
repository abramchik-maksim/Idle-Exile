using System.Collections.Generic;
using Game.Application.Ports;
using Game.Infrastructure.Configs;
using Game.Infrastructure.Repositories;
using Game.Presentation.Core.Services;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Presentation.Core.Bootstrap
{
    public sealed class RootLifetimeScope : LifetimeScope
    {
        [SerializeField] private CharacterDatabaseSO _characterDatabase;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ISaveSlotManager, FileSaveSlotManager>(Lifetime.Singleton);
            builder.RegisterInstance(new GameSessionContext());
            builder.Register<ICharacterConfigProvider>(
                _ => new ScriptableObjectCharacterConfigProvider(_characterDatabase), Lifetime.Singleton);
            builder.RegisterInstance(_characterDatabase);
            builder.Register<ISceneLoader>(_ => new SceneLoader(this), Lifetime.Singleton);
            builder.RegisterEntryPoint<RootStartupEntryPoint>();
        }
    }
}
