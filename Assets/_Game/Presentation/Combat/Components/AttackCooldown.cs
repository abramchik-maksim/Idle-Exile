using Unity.Entities;

namespace Game.Presentation.Combat.Components
{
    public struct AttackCooldown : IComponentData
    {
        public float Cooldown;
        public float Timer;
    }
}
