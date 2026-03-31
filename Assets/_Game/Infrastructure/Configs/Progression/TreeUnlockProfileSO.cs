using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Infrastructure.Configs.Progression
{
    [CreateAssetMenu(menuName = "Idle Exile/Progression/Tree Unlock Profile", fileName = "TreeUnlockProfile")]
    public sealed class TreeUnlockProfileSO : ScriptableObject
    {
        public List<LevelUnlockEntry> levels = new();

        [Serializable]
        public sealed class LevelUnlockEntry
        {
            [Min(1)] public int level = 1;
            public List<int> halfWidthsByRow = new();
        }
    }
}
