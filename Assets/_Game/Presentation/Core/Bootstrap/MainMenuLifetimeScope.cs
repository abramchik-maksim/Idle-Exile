using Game.Presentation.UI.MainMenu;
using VContainer;
using VContainer.Unity;

namespace Game.Presentation.Core.Bootstrap
{
    public sealed class MainMenuLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<MainMenuState>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<TitleScreenView>();
            builder.RegisterComponentInHierarchy<SaveSlotView>();
            builder.RegisterComponentInHierarchy<CharacterSelectView>();

            builder.RegisterEntryPoint<TitleScreenPresenter>();
            builder.RegisterEntryPoint<SaveSlotPresenter>();
            builder.RegisterEntryPoint<CharacterSelectPresenter>();
        }
    }
}
