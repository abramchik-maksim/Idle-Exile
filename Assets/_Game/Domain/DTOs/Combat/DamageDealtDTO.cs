using Game.Domain.Combat;

namespace Game.Domain.DTOs.Combat
{
    public readonly struct DamageDealtDTO
    {
        public DamageResult Result { get; }
        public bool IsHeroDamage { get; }
        public float WorldX { get; }
        public float WorldY { get; }

        public DamageDealtDTO(DamageResult result, bool isHeroDamage, float worldX = 0f, float worldY = 0f)
        {
            Result = result;
            IsHeroDamage = isHeroDamage;
            WorldX = worldX;
            WorldY = worldY;
        }
    }
}
