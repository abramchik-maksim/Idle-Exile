using System;
using System.Collections.Generic;

namespace Game.Domain.Skills.Crafting
{
    public sealed class SkillGemInventory
    {
        private readonly Dictionary<string, int> _stacks = new();

        public event Action OnChanged;

        public int RemovalCurrencyCount { get; private set; }

        public void Add(string gemDefinitionId, int count = 1)
        {
            if (!_stacks.ContainsKey(gemDefinitionId))
                _stacks[gemDefinitionId] = 0;

            _stacks[gemDefinitionId] += count;
            OnChanged?.Invoke();
        }

        public bool TryConsume(string gemDefinitionId)
        {
            if (!_stacks.TryGetValue(gemDefinitionId, out var count) || count <= 0)
                return false;

            _stacks[gemDefinitionId] = count - 1;
            if (_stacks[gemDefinitionId] <= 0)
                _stacks.Remove(gemDefinitionId);

            OnChanged?.Invoke();
            return true;
        }

        public int GetCount(string gemDefinitionId)
        {
            return _stacks.TryGetValue(gemDefinitionId, out var count) ? count : 0;
        }

        public IReadOnlyDictionary<string, int> GetAll() =>
            new Dictionary<string, int>(_stacks);

        public void AddRemovalCurrency(int count = 1)
        {
            RemovalCurrencyCount += count;
            OnChanged?.Invoke();
        }

        public bool TryConsumeRemovalCurrency()
        {
            if (RemovalCurrencyCount <= 0) return false;
            RemovalCurrencyCount--;
            OnChanged?.Invoke();
            return true;
        }
    }
}
