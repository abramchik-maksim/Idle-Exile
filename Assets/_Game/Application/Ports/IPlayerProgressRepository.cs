namespace Game.Application.Ports
{
    public interface IPlayerProgressRepository
    {
        void Save(PlayerProgressData data);
        PlayerProgressData Load();
        bool HasSave();
    }
}
