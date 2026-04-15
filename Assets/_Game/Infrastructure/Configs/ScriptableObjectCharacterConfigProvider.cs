using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Items;
using Game.Domain.SaveSystem;

namespace Game.Infrastructure.Configs
{
    public sealed class ScriptableObjectCharacterConfigProvider : ICharacterConfigProvider
    {
        private readonly Dictionary<HeroItemClass, CharacterDefinition> _cache = new();

        public ScriptableObjectCharacterConfigProvider(CharacterDatabaseSO database)
        {
            if (database == null || database.characters == null)
                return;

            foreach (var entry in database.characters)
            {
                string presetId = entry.preset != null ? entry.preset.presetId : string.Empty;
                _cache[entry.heroClass] = new CharacterDefinition(
                    entry.heroClass,
                    string.IsNullOrWhiteSpace(entry.displayName) ? entry.heroClass.ToString() : entry.displayName,
                    entry.description ?? string.Empty,
                    presetId ?? string.Empty,
                    entry.visualId,
                    entry.projectileVisualId);
            }
        }

        public IReadOnlyList<CharacterDefinition> GetAll()
        {
            var list = new List<CharacterDefinition>(_cache.Count);
            foreach (var kv in _cache)
                list.Add(kv.Value);
            return list;
        }

        public CharacterDefinition GetByClass(HeroItemClass heroClass)
        {
            return _cache.TryGetValue(heroClass, out var def)
                ? def
                : new CharacterDefinition(heroClass, heroClass.ToString(), string.Empty, string.Empty);
        }

        public string GetPresetId(HeroItemClass heroClass) => GetByClass(heroClass).PresetId;
    }
}
