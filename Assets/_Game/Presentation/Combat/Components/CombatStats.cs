using Unity.Entities;

namespace Game.Presentation.Combat.Components
{
    public struct CombatStats : IComponentData
    {
        // Core
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

        // Defense
        public float Evasion;
        public float BlockChance;
        public float LifeLeech;
        public float FireResistance;
        public float ColdResistance;
        public float LightningResistance;
        public float CorrosionResistance;

        // Offense mechanics
        public float DoubleHitChance;
        public float IgnoreArmorChance;
    }
}
