namespace Game.Application.Ports
{
    public sealed class PlayerProgressData
    {
        public int CurrentTier { get; set; }
        public int CurrentMap { get; set; }
        public int CurrentBattle { get; set; }
        public int TotalKills { get; set; }
        public string HeroId { get; set; }
    }
}
