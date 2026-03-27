using System;
using System.Collections.Generic;
using Game.Domain.Skills;
using Game.Domain.Stats;

namespace Game.Application.Skills
{
    public sealed class UtilitySkillRunner
    {
        public struct SlotState
        {
            public SkillInstance Skill;
            public float CooldownTimer;
            public float CooldownTotal;
            public float BuffTimer;
            public float BuffTotal;
            public bool IsBuffActive;
        }

        public struct ActiveBuff
        {
            public string SkillName;
            public SkillEffectType EffectType;
            public float EffectValue;
            public float RemainingTime;
            public float TotalDuration;
        }

        private readonly SlotState[] _slots = new SlotState[4];
        private readonly Dictionary<StatType, float> _buffBonuses = new();
        private float _healAccumulator;

        public event Action OnBuffsChanged;
        public event Action<float, float> OnCloneRequested;

        public void Initialize(SkillLoadout loadout)
        {
            for (int i = 0; i < 4; i++)
            {
                var skill = loadout.GetSlot(SkillLoadout.FirstUtilitySlotIndex + i);
                _slots[i] = new SlotState
                {
                    Skill = skill,
                    CooldownTimer = 0f,
                    CooldownTotal = skill?.Definition.Cooldown ?? 0f,
                    BuffTimer = 0f,
                    BuffTotal = skill?.Definition.EffectDuration ?? 0f,
                    IsBuffActive = false
                };
            }

            _buffBonuses.Clear();
            _healAccumulator = 0f;
        }

        public void Tick(float dt)
        {
            bool changed = false;

            for (int i = 0; i < 4; i++)
            {
                ref var s = ref _slots[i];
                if (s.Skill == null) continue;

                if (s.IsBuffActive)
                {
                    s.BuffTimer -= dt;
                    if (s.BuffTimer <= 0f)
                    {
                        s.IsBuffActive = false;
                        s.BuffTimer = 0f;
                        changed = true;
                    }
                }

                if (s.CooldownTimer > 0f)
                {
                    s.CooldownTimer -= dt;
                    if (s.CooldownTimer <= 0f)
                        s.CooldownTimer = 0f;
                }

                if (s.CooldownTimer <= 0f && !s.IsBuffActive)
                {
                    ActivateSkill(ref s);
                    changed = true;
                }
            }

            if (changed)
            {
                RecalculateBuffBonuses();
                OnBuffsChanged?.Invoke();
            }
        }

        private void ActivateSkill(ref SlotState s)
        {
            var def = s.Skill.Definition;
            s.CooldownTimer = def.Cooldown;
            s.CooldownTotal = def.Cooldown;

            if (def.EffectDuration > 0)
            {
                s.IsBuffActive = true;
                s.BuffTimer = def.EffectDuration;
                s.BuffTotal = def.EffectDuration;
            }
        }

        private void RecalculateBuffBonuses()
        {
            _buffBonuses.Clear();
            _healAccumulator = 0f;

            for (int i = 0; i < 4; i++)
            {
                ref var s = ref _slots[i];
                if (s.Skill == null || !s.IsBuffActive) continue;

                var def = s.Skill.Definition;
                switch (def.EffectType)
                {
                    case SkillEffectType.BuffArmor:
                        AddBonus(StatType.Armor, def.EffectValue);
                        break;
                    case SkillEffectType.BuffEvasion:
                        AddBonus(StatType.Evasion, def.EffectValue);
                        break;
                    case SkillEffectType.BuffAttackSpeed:
                        AddBonus(StatType.AttackSpeed, def.EffectValue / 100f);
                        break;
                    case SkillEffectType.HealOverTime:
                        _healAccumulator = def.EffectValue;
                        break;
                    case SkillEffectType.SummonClone:
                        OnCloneRequested?.Invoke(def.EffectValue, def.EffectDuration);
                        break;
                }
            }
        }

        private void AddBonus(StatType stat, float value)
        {
            _buffBonuses.TryAdd(stat, 0f);
            _buffBonuses[stat] += value;
        }

        public float GetCooldownNormalized(int utilityIndex)
        {
            if (utilityIndex < 0 || utilityIndex >= 4) return 0f;
            ref var s = ref _slots[utilityIndex];
            if (s.Skill == null || s.CooldownTotal <= 0f) return 0f;
            return Math.Clamp(s.CooldownTimer / s.CooldownTotal, 0f, 1f);
        }

        public float GetHealPerSecond() => _healAccumulator;

        public IReadOnlyDictionary<StatType, float> GetBuffBonuses() => _buffBonuses;

        public IReadOnlyList<ActiveBuff> GetActiveBuffs()
        {
            var result = new List<ActiveBuff>();
            for (int i = 0; i < 4; i++)
            {
                ref var s = ref _slots[i];
                if (s.Skill == null || !s.IsBuffActive) continue;

                result.Add(new ActiveBuff
                {
                    SkillName = s.Skill.Definition.Name,
                    EffectType = s.Skill.Definition.EffectType,
                    EffectValue = s.Skill.Definition.EffectValue,
                    RemainingTime = s.BuffTimer,
                    TotalDuration = s.BuffTotal
                });
            }
            return result;
        }

        public bool HasSlot(int utilityIndex)
        {
            if (utilityIndex < 0 || utilityIndex >= 4) return false;
            return _slots[utilityIndex].Skill != null;
        }
    }
}
