using Game.Domain.Combat;

namespace Game.Domain.DTOs.Combat
{
    public readonly struct DamageDealtDTO
    {
        public DamageResult Result { get; }
        public bool IsHeroDamage { get; }
        public DamageDealtDTO(DamageResult result, bool isHeroDamage)
        {
            Result = result;
            IsHeroDamage = isHeroDamage;
        }
    }
}
