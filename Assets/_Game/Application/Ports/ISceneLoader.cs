using System.Threading.Tasks;

namespace Game.Application.Ports
{
    public interface ISceneLoader
    {
        Task LoadMainMenuAsync();
        Task LoadGameplayAsync();
    }
}
