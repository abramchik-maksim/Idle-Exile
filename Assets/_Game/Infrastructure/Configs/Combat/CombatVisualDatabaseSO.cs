using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Infrastructure.Configs.Combat
{
    [CreateAssetMenu(menuName = "Idle Exile/Combat/Visual Database", fileName = "CombatVisualDatabase")]
    public sealed class CombatVisualDatabaseSO : ScriptableObject
    {
        public List<VisualEntry> entries = new();

        public GameObject GetPrefab(int visualId)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].id == visualId)
                    return entries[i].spriteViewPrefab;
            }
            return null;
        }

        public float GetScale(int visualId)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].id == visualId)
                    return entries[i].scale;
            }
            return 1f;
        }
    }

    [Serializable]
    public struct VisualEntry
    {
        public int id;
        public GameObject spriteViewPrefab;
        public float scale;
    }
}
