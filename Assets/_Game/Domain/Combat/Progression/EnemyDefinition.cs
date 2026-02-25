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

        public EnemyDefinition(
            string id, string name,
            float baseHealth, float baseDamage,
            float baseArmor, float baseSpeed)
        {
            Id = id;
            Name = name;
            BaseHealth = baseHealth;
            BaseDamage = baseDamage;
            BaseArmor = baseArmor;
            BaseSpeed = baseSpeed;
        }
    }
}
