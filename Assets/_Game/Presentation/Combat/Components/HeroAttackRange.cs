using Unity.Entities;

namespace Game.Presentation.Combat.Components
{
    public struct HeroAttackRange : IComponentData
    {
        public float Value;
        public byte IsMelee;
    }
}
