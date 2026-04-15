using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Infrastructure.ItemAffixes
{
    [CreateAssetMenu(menuName = "Idle Exile/Drop Quality Database", fileName = "DropQualityDatabase")]
    public sealed class DropQualityDatabaseSO : ScriptableObject
    {
        [Tooltip("Imported from DropQualityProgression.csv")]
        public List<DropQualityBandRow> bands = new();
    }

    [Serializable]
    public sealed class DropQualityBandRow
    {
        public int progressBand;
        public int idleStageMin;
        public int idleStageMax;
        public int allowedTierMin;
        public int allowedTierMax;
        public float tierBias;
        public float qualityMultiplier = 1f;
        public int weightNormal;
        public int weightMagic;
        public int weightRare;
        public int weightMythic;
    }
}
