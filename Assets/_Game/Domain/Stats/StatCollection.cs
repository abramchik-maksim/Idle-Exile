using System.Collections.Generic;
using System.Linq;

namespace Game.Domain.Stats
{
    /// <summary>
    /// PoE-style stat aggregation: Base + Flat → × (1 + sum of Increased%) → × each More multiplier.
    /// </summary>
    public sealed class StatCollection
    {
        private readonly Dictionary<StatType, float> _baseValues = new();
        private readonly List<Modifier> _modifiers = new();

        public IReadOnlyList<Modifier> Modifiers => _modifiers;

        public void SetBase(StatType stat, float value) => _baseValues[stat] = value;

        public float GetBase(StatType stat) =>
            _baseValues.TryGetValue(stat, out var v) ? v : 0f;

        public void AddModifier(Modifier mod) => _modifiers.Add(mod);

        public void RemoveModifiersBySource(string source) =>
            _modifiers.RemoveAll(m => m.Source == source);

        public void ClearModifiers() => _modifiers.Clear();

        public float GetFinal(StatType stat)
        {
            float baseVal = GetBase(stat);

            float flatSum = 0f;
            float increasedSum = 0f;
            float moreProduct = 1f;

            foreach (var mod in _modifiers)
            {
                if (mod.Stat != stat) continue;

                switch (mod.Type)
                {
                    case ModifierType.Flat:
                        flatSum += mod.Value;
                        break;
                    case ModifierType.Increased:
                        increasedSum += mod.Value;
                        break;
                    case ModifierType.More:
                        moreProduct *= 1f + mod.Value;
                        break;
                }
            }

            return (baseVal + flatSum) * (1f + increasedSum) * moreProduct;
        }

        public Dictionary<StatType, float> GetAllFinal()
        {
            var statTypes = _baseValues.Keys
                .Union(_modifiers.Select(m => m.Stat))
                .Distinct();

            var result = new Dictionary<StatType, float>();
            foreach (var st in statTypes)
                result[st] = GetFinal(st);
            return result;
        }
    }
}
