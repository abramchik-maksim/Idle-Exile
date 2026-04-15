using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Infrastructure.Configs.Combat
{
    [CreateAssetMenu(menuName = "Idle Exile/Combat/Tier Definition", fileName = "NewTier")]
    public sealed class TierDefinitionSO : ScriptableObject
    {
        public string id;
        public string displayName;
        public float scaling = 1f;

        [Tooltip("If true, the first map in the list is forced (no player choice)")]
        public bool hasForcedStartMap;

        [Header("Maps")]
        [Tooltip("The maps that belong to this tier (linear sequence)")]
        public List<MapDefinitionSO> maps = new();

        [Tooltip("Map choice points: each entry offers 2 map options")]
        public List<MapChoiceDataSO> mapChoices = new();
    }

    [Serializable]
    public sealed class MapChoiceDataSO
    {
        public MapDefinitionSO option1;
        public MapDefinitionSO option2;
    }
}
