using System;

namespace Game.Domain.Combat
{
    public static class AilmentCalculator
    {
        public const float IgniteTotalDamagePercent = 3.0f;
        public const float IgniteDuration = 3f;
        public const float AilmentTickInterval = 0.5f;
        public const int IgniteTickCount = 6;

        public const float BleedDamagePercent = 0.6f;
        public const float BleedDuration = 5f;
        public const int BleedTickCount = 10;

        public const int MaxChillStacks = 10;
        public const int MaxShockStacks = 10;
        public const float ChillSlowPerStack = 0.05f;
        public const float ShockAmpPerStack = 0.05f;
        public const float FreezeDuration = 3f;

        public static float GetIgniteDamagePerTick(float hitDamage)
        {
            return hitDamage * IgniteTotalDamagePercent / IgniteTickCount;
        }

        public static float GetBleedDamagePerTick(float hitDamage)
        {
            return hitDamage * BleedDamagePercent / BleedTickCount;
        }

        public static float GetChillSlowFactor(int stacks)
        {
            int clamped = Math.Clamp(stacks, 0, MaxChillStacks);
            return 1f - clamped * ChillSlowPerStack;
        }

        public static float GetShockDamageMultiplier(int stacks)
        {
            int clamped = Math.Clamp(stacks, 0, MaxShockStacks);
            return 1f + clamped * ShockAmpPerStack;
        }

        public static bool ShouldFreeze(int chillStacks)
        {
            return chillStacks >= MaxChillStacks;
        }
    }
}
