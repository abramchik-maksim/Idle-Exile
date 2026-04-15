using System;
using System.Collections.Generic;
using Game.Domain.Items;
using UnityEngine;

namespace Game.Infrastructure.Configs
{
    [CreateAssetMenu(menuName = "Idle Exile/Characters/Character Database", fileName = "CharacterDatabase")]
    public sealed class CharacterDatabaseSO : ScriptableObject
    {
        public List<CharacterEntry> characters = new();
    }

    [Serializable]
    public struct CharacterEntry
    {
        public HeroItemClass heroClass;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public StartingPresetSO preset;
        [Tooltip("Index into CombatVisualDatabaseSO entries")]
        public int visualId;
        [Tooltip("Index into CombatVisualDatabaseSO entries for projectile sprite")]
        public int projectileVisualId;
    }
}
