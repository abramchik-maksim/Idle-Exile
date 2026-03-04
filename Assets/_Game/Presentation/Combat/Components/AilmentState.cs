using Unity.Entities;

namespace Game.Presentation.Combat.Components
{
    public struct AilmentState : IComponentData
    {
        public int ChillStacks;
        public int ShockStacks;

        public float IgniteDamagePerTick;
        public float IgniteTimer;

        public float BleedTotalDps;
    }

    public struct BleedStack : IBufferElementData
    {
        public float DamagePerTick;
        public float RemainingDuration;
    }
}
