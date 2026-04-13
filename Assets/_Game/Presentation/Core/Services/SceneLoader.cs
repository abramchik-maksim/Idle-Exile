using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;
using Game.Application.Ports;

namespace Game.Presentation.Core.Services
{
    public sealed class SceneLoader : ISceneLoader
    {
        private readonly LifetimeScope _rootScope;

        public SceneLoader(LifetimeScope rootScope)
        {
            _rootScope = rootScope;
        }

        public async Task LoadMainMenuAsync()
        {
            using (LifetimeScope.EnqueueParent(_rootScope))
            {
                await SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single)
                    .ToUniTask();
            }
        }

        public async Task LoadGameplayAsync()
        {
            using (LifetimeScope.EnqueueParent(_rootScope))
            {
                await SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single)
                    .ToUniTask();
            }
        }
    }
}
