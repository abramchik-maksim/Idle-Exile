using Game.Domain.Combat;
using Unity.Entities;

namespace Game.Presentation.Combat.Components
{
    public struct EnemyBehavior : IComponentData
    {
        public EnemyArchetype Archetype;
        public float AttackRange;
    }
}
