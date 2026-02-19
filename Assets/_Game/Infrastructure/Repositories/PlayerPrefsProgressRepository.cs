using Game.Application.Ports;
using UnityEngine;

namespace Game.Infrastructure.Repositories
{
    public sealed class PlayerPrefsProgressRepository : IPlayerProgressRepository
    {
        private const string Key = "player_progress";

        public void Save(PlayerProgressData data)
        {
            string json = JsonUtility.ToJson(new SerializableProgress
            {
                currentWave = data.CurrentWave,
                totalKills = data.TotalKills,
                heroId = data.HeroId
            });
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }

        public PlayerProgressData Load()
        {
            if (!HasSave())
                return new PlayerProgressData { CurrentWave = 0, TotalKills = 0, HeroId = "default_hero" };

            var s = JsonUtility.FromJson<SerializableProgress>(PlayerPrefs.GetString(Key));
            return new PlayerProgressData
            {
                CurrentWave = s.currentWave,
                TotalKills = s.totalKills,
                HeroId = s.heroId
            };
        }

        public bool HasSave() => PlayerPrefs.HasKey(Key);

        [System.Serializable]
        private struct SerializableProgress
        {
            public int currentWave;
            public int totalKills;
            public string heroId;
        }
    }
}
