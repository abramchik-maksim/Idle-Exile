using System;
using System.Collections.Generic;
using Game.Domain.Combat.Progression;
using UnityEngine;

namespace Game.Infrastructure.Configs.Combat
{
    [CreateAssetMenu(menuName = "Idle Exile/Combat/Map Definition", fileName = "NewMap")]
    public sealed class MapDefinitionSO : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(1, 3)]
        public string description;
        [Tooltip("Addressable key for the location prefab to load for this map")]
        public string locationId;
        public List<BattleDefinitionSO> battles = new();

        [Header("Loot Bias")]
        public float itemWeightMultiplier = 1f;
        public float currencyWeightMultiplier = 1f;

        [Header("Map Modifiers")]
        public List<MapModifierDataSO> modifiers = new();

        [Tooltip("If true, the last battle on this map is a boss encounter")]
        public bool isBossMap;
    }

    [Serializable]
    public struct MapModifierDataSO
    {
        public MapModifierType type;
        public float value;
    }
}
