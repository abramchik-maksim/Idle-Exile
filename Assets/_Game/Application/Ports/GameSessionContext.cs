using Game.Domain.Items;

namespace Game.Application.Ports
{
    public sealed class GameSessionContext
    {
        public int SaveSlotIndex { get; set; } = -1;
        public HeroItemClass SelectedClass { get; set; } = HeroItemClass.Warrior;
        public bool IsNewGame { get; set; }
    }
}
