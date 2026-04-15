namespace Game.Domain.Combat.Progression
{
    public sealed class EnemyDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public float BaseHealth { get; }
        public float BaseDamage { get; }
        public float BaseArmor { get; }
        public float BaseSpeed { get; }
        public EnemyArchetype Archetype { get; }
        public float AttackRange { get; }
        public float AttackSpeed { get; }
        public SpellDefinition Spell { get; }
        public int VisualId { get; }
        public int ProjectileVisualId { get; }

        public EnemyDefinition(
            string id, string name,
            float baseHealth, float baseDamage,
            float baseArmor, float baseSpeed,
            EnemyArchetype archetype = EnemyArchetype.Melee,
            float attackRange = 1f,
            float attackSpeed = 0.8f,
            SpellDefinition spell = null,
            int visualId = 0,
            int projectileVisualId = 0)
        {
            Id = id;
            Name = name;
            BaseHealth = baseHealth;
            BaseDamage = baseDamage;
            BaseArmor = baseArmor;
            BaseSpeed = baseSpeed;
            Archetype = archetype;
            AttackRange = attackRange;
            AttackSpeed = attackSpeed;
            Spell = spell;
            VisualId = visualId;
            ProjectileVisualId = projectileVisualId;
        }
    }
}
