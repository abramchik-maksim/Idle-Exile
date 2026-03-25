using Game.Domain.Combat;
using Unity.Entities;

namespace Game.Presentation.Combat.Components
{
    public struct HeroSkillAffixData : IComponentData
    {
        public float IgniteChance;
        public float ChillChance;
        public float ShockChance;
        public float BleedChance;

        public float GainAsFirePercent;
        public float GainAsColdPercent;
        public float GainAsLightningPercent;

        public float AoEAilmentChance;
        public float AoEAilmentRadius;
        public AilmentType AoEAilmentType;
    }
}
