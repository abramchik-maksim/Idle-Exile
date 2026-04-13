using System;
using System.Collections.Generic;
using System.IO;
using Game.Application.Ports;
using Game.Domain.Items;
using Game.Domain.SaveSystem;
using UnityEngine;

namespace Game.Infrastructure.Repositories
{
    public sealed class FileSaveSlotManager : ISaveSlotManager
    {
        private int _activeSlotIndex;

        public int ActiveSlotIndex => _activeSlotIndex;

        public IReadOnlyList<SaveSlotMetadata> GetAllSlots()
        {
            var result = new List<SaveSlotMetadata>(SaveConstants.MaxSlots);
            for (int i = 0; i < SaveConstants.MaxSlots; i++)
                result.Add(GetSlot(i));
            return result;
        }

        public SaveSlotMetadata GetSlot(int index)
        {
            EnsureValidSlotIndex(index);
            string path = FileSavePaths.MetadataPath(index);
            if (!File.Exists(path))
            {
                return new SaveSlotMetadata(
                    index, true, string.Empty, HeroItemClass.Warrior, 1, 0, 0, 0L);
            }

            try
            {
                var json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveSlotMetadataData>(json);
                if (data == null)
                    return new SaveSlotMetadata(index, true, string.Empty, HeroItemClass.Warrior, 1, 0, 0, 0L);

                return new SaveSlotMetadata(
                    index,
                    data.isEmpty,
                    data.heroId ?? string.Empty,
                    data.heroClass,
                    data.level <= 0 ? 1 : data.level,
                    data.currentTier,
                    data.currentMap,
                    data.lastPlayedTicks);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FileSaveSlotManager] Failed to read metadata for slot {index}: {ex.Message}");
                return new SaveSlotMetadata(index, true, string.Empty, HeroItemClass.Warrior, 1, 0, 0, 0L);
            }
        }

        public void SetActiveSlot(int index)
        {
            EnsureValidSlotIndex(index);
            _activeSlotIndex = index;
        }

        public void CreateSlot(int index, string heroId, HeroItemClass heroClass)
        {
            EnsureValidSlotIndex(index);
            EnsureSlotDirectory(index);

            var metadata = new SaveSlotMetadata(
                index,
                false,
                heroId ?? "default_hero",
                heroClass,
                1,
                0,
                0,
                DateTime.UtcNow.Ticks);
            UpdateMetadata(metadata);
        }

        public void DeleteSlot(int index)
        {
            EnsureValidSlotIndex(index);
            string dir = FileSavePaths.SlotDirectory(index);
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }

        public void UpdateMetadata(SaveSlotMetadata metadata)
        {
            EnsureValidSlotIndex(metadata.SlotIndex);
            EnsureSlotDirectory(metadata.SlotIndex);

            var data = new SaveSlotMetadataData
            {
                isEmpty = metadata.IsEmpty,
                heroId = metadata.HeroId ?? string.Empty,
                heroClass = metadata.HeroClass,
                level = metadata.Level <= 0 ? 1 : metadata.Level,
                currentTier = metadata.CurrentTier,
                currentMap = metadata.CurrentMap,
                lastPlayedTicks = metadata.LastPlayedTicks == 0 ? DateTime.UtcNow.Ticks : metadata.LastPlayedTicks
            };

            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FileSavePaths.MetadataPath(metadata.SlotIndex), json);
        }

        private static void EnsureValidSlotIndex(int index)
        {
            if (index < 0 || index >= SaveConstants.MaxSlots)
                throw new ArgumentOutOfRangeException(nameof(index), $"Slot index must be in range [0..{SaveConstants.MaxSlots - 1}]");
        }

        private static void EnsureSlotDirectory(int index)
        {
            var dir = FileSavePaths.SlotDirectory(index);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        [Serializable]
        private sealed class SaveSlotMetadataData
        {
            public bool isEmpty;
            public string heroId;
            public HeroItemClass heroClass;
            public int level;
            public int currentTier;
            public int currentMap;
            public long lastPlayedTicks;
        }
    }
}
