using Unity.Entities;

namespace Game.Presentation.Combat.Components
{
    public struct CombatStats : IComponentData
    {
        public float MaxHealth;
        public float CurrentHealth;
        public float PhysicalDamage;
        public float FireDamage;
        public float ColdDamage;
        public float LightningDamage;
        public float CriticalChance;
        public float CriticalMultiplier;
        public float AttackSpeed;
        public float Armor;
        public float MoveSpeed;
    }
}
