using Game.Domain.Items;

namespace Game.Domain.SaveSystem
{
    public readonly struct SaveSlotMetadata
    {
        public int SlotIndex { get; }
        public bool IsEmpty { get; }
        public string HeroId { get; }
        public HeroItemClass HeroClass { get; }
        public int Level { get; }
        public int CurrentTier { get; }
        public int CurrentMap { get; }
        public long LastPlayedTicks { get; }

        public SaveSlotMetadata(
            int slotIndex,
            bool isEmpty,
            string heroId,
            HeroItemClass heroClass,
            int level,
            int currentTier,
            int currentMap,
            long lastPlayedTicks)
        {
            SlotIndex = slotIndex;
            IsEmpty = isEmpty;
            HeroId = heroId;
            HeroClass = heroClass;
            Level = level;
            CurrentTier = currentTier;
            CurrentMap = currentMap;
            LastPlayedTicks = lastPlayedTicks;
        }
    }
}
