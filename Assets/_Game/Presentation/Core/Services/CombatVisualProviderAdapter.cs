using UnityEngine;
using Game.Infrastructure.Configs.Combat;
using Game.Presentation.Combat.Rendering;

namespace Game.Presentation.Core.Services
{
    public sealed class CombatVisualProviderAdapter : ICombatVisualProvider
    {
        private readonly CombatVisualDatabaseSO _db;

        public CombatVisualProviderAdapter(CombatVisualDatabaseSO db)
        {
            _db = db;
        }

        public GameObject GetPrefab(int visualId) => _db.GetPrefab(visualId);
        public float GetScale(int visualId) => _db.GetScale(visualId);
    }
}
