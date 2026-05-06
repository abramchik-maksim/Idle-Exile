using System;
using System.Collections.Generic;
using MessagePipe;
using VContainer.Unity;
using Game.Application.Inventory;
using Game.Application.Loot;
using Game.Application.Ports;
using Game.Domain.DTOs.Inventory;
using Game.Domain.DTOs.Progression;
using Game.Domain.Items;
using Game.Domain.Skills.Crafting;
using Game.Domain.Stats;
using Game.Presentation.UI.Cheats;
using UnityEngine;

namespace Game.Presentation.UI.Presenters
{
    public sealed class CheatsPresenter : IStartable, IDisposable
    {
        private readonly CheatsView _cheatsView;
        private readonly InventoryCommandService _inventoryCommands;
        private readonly ItemRollingService _itemRolling;
        private readonly IConfigProvider _config;
        private readonly IAffixConfigProvider _affix;
        private readonly IItemAffixModifierResolver _affixResolver;
        private readonly IGameStateProvider _gameState;
        private readonly ISkillGemConfigProvider _gemConfig;
        private readonly SkillGemInventory _gemInventory;
        private readonly IRandomService _random;
        private readonly IPublisher<InventoryChangedDTO> _inventoryChangedPub;
        private readonly IPublisher<ItemAddedDTO> _itemAddedPub;
        private readonly IPublisher<TreeXpChangedDTO> _treeXpChangedPub;
        private readonly IPublisher<TreeLevelChangedDTO> _treeLevelChangedPub;
        private readonly IPublisher<TreeTalentsChangedDTO> _treeTalentsChangedPub;

        public CheatsPresenter(
            CheatsView cheatsView,
            InventoryCommandService inventoryCommands,
            ItemRollingService itemRolling,
            IConfigProvider config,
            IAffixConfigProvider affix,
            IItemAffixModifierResolver affixResolver,
            IGameStateProvider gameState,
            ISkillGemConfigProvider gemConfig,
            SkillGemInventory gemInventory,
            IRandomService random,
            IPublisher<InventoryChangedDTO> inventoryChangedPub,
            IPublisher<ItemAddedDTO> itemAddedPub,
            IPublisher<TreeXpChangedDTO> treeXpChangedPub,
            IPublisher<TreeLevelChangedDTO> treeLevelChangedPub,
            IPublisher<TreeTalentsChangedDTO> treeTalentsChangedPub)
        {
            _cheatsView = cheatsView;
            _inventoryCommands = inventoryCommands;
            _itemRolling = itemRolling;
            _config = config;
            _affix = affix;
            _affixResolver = affixResolver;
            _gameState = gameState;
            _gemConfig = gemConfig;
            _gemInventory = gemInventory;
            _random = random;
            _inventoryChangedPub = inventoryChangedPub;
            _itemAddedPub = itemAddedPub;
            _treeXpChangedPub = treeXpChangedPub;
            _treeLevelChangedPub = treeLevelChangedPub;
            _treeTalentsChangedPub = treeTalentsChangedPub;
        }

        public void Start()
        {
            _cheatsView.OnGenerateItemClicked += HandleGenerateItem;
            _cheatsView.OnGenerateBowProcClicked += HandleGenerateBowProcItem;
            _cheatsView.OnAddSkillGemClicked += HandleAddSkillGem;
            _cheatsView.OnAddRemovalOrbClicked += HandleAddRemovalOrb;
            _cheatsView.OnAddTreeXpClicked += HandleAddTreeXp;
            _cheatsView.OnResetSaveClicked += HandleResetSave;

            Debug.Log("[CheatsPresenter] Initialized.");
        }

        private void HandleGenerateItem()
        {
            var item = _itemRolling.RollRandomItem(1);
            if (item == null)
            {
                _cheatsView.SetFeedback("No item definitions available.");
                return;
            }

            var inventory = _gameState.Inventory;
            if (!_inventoryCommands.TryAddItem(inventory, item))
            {
                _cheatsView.SetFeedback("Inventory is full!");
                return;
            }

            _itemAddedPub.Publish(new ItemAddedDTO(item.Uid, item.Definition.Id));
            _inventoryChangedPub.Publish(new InventoryChangedDTO());

            var def = item.Definition;
            string rarityTag = item.Rarity != Rarity.Normal ? $" [{item.Rarity}]" : "";
            int rolledCount = item.RolledAffixes.Count > 0 ? item.RolledAffixes.Count : item.RolledModifiers.Count;
            _cheatsView.SetFeedback($"Added: {def.Name}{rarityTag}\n+{rolledCount} rolled lines");
            Debug.Log($"[CheatsPresenter] Generated item: {def.Name} ({def.Slot}) rolledLines={rolledCount}");
        }

        private void HandleAddSkillGem()
        {
            var allGems = _gemConfig.GetAllGems();
            if (allGems.Count == 0)
            {
                _cheatsView.SetFeedback("No gem definitions available.");
                return;
            }

            int index = _random.Next(0, allGems.Count);
            var gem = allGems[index];

            _gemInventory.Add(gem.Id, 3);
            _cheatsView.SetFeedback($"Added 3x {gem.Name}\n({gem.Element} / {gem.Level})");
            Debug.Log($"[CheatsPresenter] Added 3x {gem.Name} ({gem.Id})");
        }

        private void HandleGenerateBowProcItem()
        {
            var all = _config.GetAllItems();
            if (all.Count == 0)
            {
                _cheatsView.SetFeedback("No item definitions available.");
                return;
            }

            ItemDefinition bow = null;
            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                if (d.Slot == EquipmentSlotType.MainHand && d.WeaponType == Game.Domain.Skills.WeaponType.Bow)
                {
                    bow = d;
                    break;
                }
            }

            if (bow == null)
            {
                _cheatsView.SetFeedback("No Bow item definition found.");
                return;
            }

            const string modFork = "Ranged_ForkChance";
            const string modPierce = "Ranged_PierceChance";
            const string modChain = "Ranged_ChainChance";
            string[] allBowProcMods = { modFork, modPierce, modChain };

            bool rollThree = _random.Next(0, 2) == 0;
            var selectedMods = new List<string>(3);
            if (rollThree)
            {
                selectedMods.Add(modFork);
                selectedMods.Add(modPierce);
                selectedMods.Add(modChain);
            }
            else
            {
                int a = _random.Next(0, 3);
                int b;
                do
                {
                    b = _random.Next(0, 3);
                } while (b == a);

                selectedMods.Add(allBowProcMods[a]);
                selectedMods.Add(allBowProcMods[b]);
            }

            var slot = bow.Slot.NormalizeForAffixRules();
            var rolledAffixes = new List<RolledItemAffix>();
            var modsOut = new List<Game.Domain.Stats.Modifier>();

            foreach (var chosenMod in selectedMods)
            {
                var candidates = new List<ItemAffixPoolEntry>();
                foreach (var e in _affix.PoolEntries)
                {
                    if (e.ModId != chosenMod) continue;
                    if (!_affix.IsModAllowedOnSlot(e.ModId, slot)) continue;
                    candidates.Add(e);
                }

                if (candidates.Count == 0)
                {
                    _cheatsView.SetFeedback($"No affix pool entries for {chosenMod}.");
                    return;
                }

                var entry = candidates[_random.Next(0, candidates.Count)];
                float raw = _random.NextFloat(entry.Min, entry.Max);
                float rolled = Game.Domain.Items.AffixRolledValueNormalizer.Normalize(entry.ModId, entry.ValueFormat, raw);

                var rolledAffix = new RolledItemAffix(entry.AffixId, entry.ModId, entry.Tier, rolled, entry.ValueFormat);
                rolledAffixes.Add(rolledAffix);
                foreach (var m in _affixResolver.ResolveModifiers(rolledAffix))
                    modsOut.Add(m);
            }

            var rarity = rollThree ? Rarity.Rare : Rarity.Magic;
            var item = new ItemInstance(bow, rarity, rolledAffixes, modsOut);

            var inventory = _gameState.Inventory;
            if (!_inventoryCommands.TryAddItem(inventory, item))
            {
                _cheatsView.SetFeedback("Inventory is full!");
                return;
            }

            _itemAddedPub.Publish(new ItemAddedDTO(item.Uid, item.Definition.Id));
            _inventoryChangedPub.Publish(new InventoryChangedDTO());

            string modeLabel = rollThree ? "Fork+Pierce+Chain" : "two procs";
            string lines = string.Join("\n", SummarizeBowProcLines(rolledAffixes));
            _cheatsView.SetFeedback($"Added: {bow.Name} [{rarity}] ({modeLabel})\n{lines}");
            Debug.Log($"[CheatsPresenter] Bow proc cheat: {modeLabel}, lines={rolledAffixes.Count}");
        }

        private static string[] SummarizeBowProcLines(IReadOnlyList<RolledItemAffix> affixes)
        {
            var lines = new string[affixes.Count];
            for (int i = 0; i < affixes.Count; i++)
            {
                var a = affixes[i];
                string shortName = a.ModId switch
                {
                    "Ranged_ForkChance" => "Fork",
                    "Ranged_PierceChance" => "Pierce",
                    "Ranged_ChainChance" => "Chain",
                    _ => a.ModId
                };
                lines[i] = $"+ {shortName} T{a.Tier}";
            }

            return lines;
        }

        private void HandleAddRemovalOrb()
        {
            _gemInventory.AddRemovalCurrency(5);
            _cheatsView.SetFeedback($"Added 5 Removal Orbs\nTotal: {_gemInventory.RemovalCurrencyCount}");
            Debug.Log($"[CheatsPresenter] Added 5 Removal Orbs. Total: {_gemInventory.RemovalCurrencyCount}");
        }

        private void HandleAddTreeXp()
        {
            const int amount = 25;
            var tree = _gameState.TreeTalents;
            if (tree == null)
            {
                _cheatsView.SetFeedback("Tree talents state is not initialized.");
                return;
            }

            var beforeLevel = tree.Level;
            tree.GainXp(amount);

            _treeXpChangedPub.Publish(new TreeXpChangedDTO(tree.CurrentXp, tree.XpToNextLevel));
            if (tree.Level != beforeLevel)
                _treeLevelChangedPub.Publish(new TreeLevelChangedDTO(tree.Level));
            _treeTalentsChangedPub.Publish(new TreeTalentsChangedDTO());

            _cheatsView.SetFeedback($"Added {amount} Tree XP\nLevel: {tree.Level} | XP: {tree.CurrentXp}/{tree.XpToNextLevel}");
        }

        private void HandleResetSave()
        {
            _gameState.Inventory.ClearAll();
            _gameState.Hero.Stats.ClearModifiers();

            var progress = _gameState.Progress;
            progress.CurrentTier = 0;
            progress.CurrentMap = 0;
            progress.CurrentBattle = 0;
            progress.TotalKills = 0;

            _inventoryChangedPub.Publish(new InventoryChangedDTO());

            _cheatsView.SetFeedback("Save data cleared!\nRestart to apply.");
            Debug.Log("[CheatsPresenter] All save data has been reset.");
        }

        public void Dispose()
        {
            _cheatsView.OnGenerateItemClicked -= HandleGenerateItem;
            _cheatsView.OnGenerateBowProcClicked -= HandleGenerateBowProcItem;
            _cheatsView.OnAddSkillGemClicked -= HandleAddSkillGem;
            _cheatsView.OnAddRemovalOrbClicked -= HandleAddRemovalOrb;
            _cheatsView.OnAddTreeXpClicked -= HandleAddTreeXp;
            _cheatsView.OnResetSaveClicked -= HandleResetSave;
        }
    }
}
