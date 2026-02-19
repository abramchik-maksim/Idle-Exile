namespace Game.Application.Ports
{
    public interface IPlayerProgressRepository
    {
        void Save(PlayerProgressData data);
        PlayerProgressData Load();
        bool HasSave();
    }

    public sealed class PlayerProgressData
    {
        public int CurrentWave { get; set; }
        public int TotalKills { get; set; }
        public string HeroId { get; set; }
    }
}
