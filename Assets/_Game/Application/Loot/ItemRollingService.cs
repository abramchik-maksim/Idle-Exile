using System;
using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Items;
using Game.Domain.Stats;

namespace Game.Application.Loot
{
    public sealed class ItemRollingService
    {
        private readonly IConfigProvider _config;
        private readonly IAffixConfigProvider _affix;
        private readonly IRandomService _random;
        private readonly IHeroItemClassProvider _heroClass;
        private readonly IItemAffixModifierResolver _resolver;
        private readonly IDropQualityProvider _dropQuality;

        public ItemRollingService(
            IConfigProvider config,
            IAffixConfigProvider affix,
            IRandomService random,
            IHeroItemClassProvider heroClass,
            IItemAffixModifierResolver resolver,
            IDropQualityProvider dropQuality)
        {
            _config = config;
            _affix = affix;
            _random = random;
            _heroClass = heroClass;
            _resolver = resolver;
            _dropQuality = dropQuality;
        }

        public ItemInstance RollRandomItem(int globalStage)
        {
            var allItems = _config.GetAllItems();
            if (allItems.Count == 0) return null;

            var def = allItems[_random.Next(0, allItems.Count)];
            return RollItem(def, globalStage);
        }

        public ItemInstance RollItem(ItemDefinition def, int globalStage)
        {
            var band = _dropQuality.GetBandForStage(globalStage);
            var rarity = RollRarity(band);
            int count = RollAffixCount(rarity);
            var slot = def.Slot.NormalizeForAffixRules();
            var hero = _heroClass.GetHeroItemClass();

            var affixes = new List<RolledItemAffix>();
            var mods = new List<Modifier>();
            var usedModIds = new HashSet<string>(StringComparer.Ordinal);

            if (_affix.PoolEntries.Count > 0 && count > 0)
            {
                var candidates = new List<ItemAffixPoolEntry>();
                foreach (var e in _affix.PoolEntries)
                {
                    if (!ClassMatches(e.ClassSpecific, hero)) continue;
                    if (!_affix.IsModAllowedOnSlot(e.ModId, slot)) continue;
                    if (e.Tier < band.AllowedTierMin || e.Tier > band.AllowedTierMax) continue;
                    candidates.Add(e);
                }

                int guard = 0;
                while (affixes.Count < count && guard++ < count * 80)
                {
                    if (candidates.Count == 0) break;

                    int idx = PickWeightedIndex(candidates, slot, out float weight);
                    if (weight <= 0f || idx < 0) break;

                    var entry = candidates[idx];
                    if (!usedModIds.Add(entry.ModId))
                    {
                        candidates.RemoveAt(idx);
                        continue;
                    }

                    float raw = _random.NextFloat(entry.Min, entry.Max);
                    float rolled = AffixRolledValueNormalizer.Normalize(entry.ModId, entry.ValueFormat, raw);
                    var rolledAffix = new RolledItemAffix(
                        entry.AffixId,
                        entry.ModId,
                        entry.Tier,
                        rolled,
                        entry.ValueFormat);

                    affixes.Add(rolledAffix);
                    foreach (var m in _resolver.ResolveModifiers(rolledAffix))
                        mods.Add(m);

                    candidates.RemoveAll(c => c.ModId == entry.ModId);
                }
            }

            return new ItemInstance(def, rarity, affixes, mods);
        }

        private Rarity RollRarity(DropQualityBand band)
        {
            int total = band.TotalRarityWeight;
            if (total <= 0) return Rarity.Normal;

            int roll = _random.Next(0, total);
            int acc = band.WeightNormal;
            if (roll < acc) return Rarity.Normal;

            acc += band.WeightMagic;
            if (roll < acc) return Rarity.Magic;

            acc += band.WeightRare;
            if (roll < acc) return Rarity.Rare;

            return Rarity.Mythic;
        }

        private int PickWeightedIndex(List<ItemAffixPoolEntry> candidates, EquipmentSlotType slot, out float pickedWeight)
        {
            float total = 0f;
            var weights = new float[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                var e = candidates[i];
                float w = e.Weight * _affix.GetSlotWeightMultiplier(e.ModId, slot);
                weights[i] = w;
                total += w;
            }

            pickedWeight = 0f;
            if (total <= 0f) return -1;

            float roll = _random.NextFloat(0f, total);
            float acc = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                acc += weights[i];
                if (roll <= acc)
                {
                    pickedWeight = weights[i];
                    return i;
                }
            }

            return candidates.Count - 1;
        }

        private static bool ClassMatches(string classSpecific, HeroItemClass hero)
        {
            if (string.IsNullOrWhiteSpace(classSpecific)) return true;
            return string.Equals(classSpecific.Trim(), hero.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private int RollAffixCount(Rarity rarity) =>
            rarity switch
            {
                Rarity.Normal => 0,
                Rarity.Magic => _random.Next(1, 3),
                Rarity.Rare => _random.Next(3, 5),
                Rarity.Mythic => 5,
                Rarity.Unique => 0,
                _ => 0
            };
    }
}
