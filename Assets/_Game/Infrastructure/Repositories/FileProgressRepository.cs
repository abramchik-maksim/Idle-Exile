using System;
using System.IO;
using Game.Application.Ports;
using UnityEngine;

namespace Game.Infrastructure.Repositories
{
    public sealed class FileProgressRepository : IPlayerProgressRepository
    {
        private readonly ISaveSlotManager _slotManager;

        public FileProgressRepository(ISaveSlotManager slotManager)
        {
            _slotManager = slotManager;
        }

        public void Save(PlayerProgressData data)
        {
            int slot = _slotManager.ActiveSlotIndex;
            Directory.CreateDirectory(FileSavePaths.SlotDirectory(slot));
            var json = JsonUtility.ToJson(new SerializableProgress
            {
                currentTier = data.CurrentTier,
                currentMap = data.CurrentMap,
                currentBattle = data.CurrentBattle,
                totalKills = data.TotalKills,
                heroId = data.HeroId
            }, true);
            File.WriteAllText(FileSavePaths.ProgressPath(slot), json);
        }

        public PlayerProgressData Load()
        {
            string path = FileSavePaths.ProgressPath(_slotManager.ActiveSlotIndex);
            if (!File.Exists(path))
                return CreateDefault();

            try
            {
                var json = File.ReadAllText(path);
                var s = JsonUtility.FromJson<SerializableProgress>(json);
                if (s == null) return CreateDefault();

                return new PlayerProgressData
                {
                    CurrentTier = s.currentTier,
                    CurrentMap = s.currentMap,
                    CurrentBattle = s.currentBattle,
                    TotalKills = s.totalKills,
                    HeroId = string.IsNullOrWhiteSpace(s.heroId) ? "default_hero" : s.heroId
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FileProgressRepository] Failed to load progress: {ex.Message}");
                return CreateDefault();
            }
        }

        public bool HasSave() => File.Exists(FileSavePaths.ProgressPath(_slotManager.ActiveSlotIndex));

        private static PlayerProgressData CreateDefault()
        {
            return new PlayerProgressData
            {
                CurrentTier = 0,
                CurrentMap = 0,
                CurrentBattle = 0,
                TotalKills = 0,
                HeroId = "default_hero"
            };
        }

        [Serializable]
        private sealed class SerializableProgress
        {
            public int currentTier;
            public int currentMap;
            public int currentBattle;
            public int totalKills;
            public string heroId;
        }
    }
}
