using System.Collections.Generic;
using Game.Domain.Items;
using Game.Domain.SaveSystem;

namespace Game.Application.Ports
{
    public interface ISaveSlotManager
    {
        int ActiveSlotIndex { get; }
        IReadOnlyList<SaveSlotMetadata> GetAllSlots();
        SaveSlotMetadata GetSlot(int index);
        void SetActiveSlot(int index);
        void CreateSlot(int index, string heroId, HeroItemClass heroClass);
        void DeleteSlot(int index);
        void UpdateMetadata(SaveSlotMetadata metadata);
    }
}
