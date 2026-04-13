using Cysharp.Threading.Tasks;
using Game.Application.Ports;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace Game.Presentation.Core.Bootstrap
{
    public sealed class RootStartupEntryPoint : IStartable
    {
        private readonly ISceneLoader _sceneLoader;

        public RootStartupEntryPoint(ISceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public void Start()
        {
            if (SceneManager.GetActiveScene().name == "Boot")
                LoadInitialScene().Forget();
        }

        private async UniTaskVoid LoadInitialScene()
        {
            await _sceneLoader.LoadMainMenuAsync();
        }
    }
}
