using System;
using System.Collections.Generic;
using MessagePipe;
using VContainer.Unity;
using Game.Application.Inventory;
using Game.Application.Loot;
using Game.Application.Ports;
using Game.Domain.DTOs.Inventory;
using Game.Domain.Items;
using Game.Domain.Skills.Crafting;
using Game.Presentation.UI.Cheats;
using UnityEngine;

namespace Game.Presentation.UI.Presenters
{
    public sealed class CheatsPresenter : IStartable, IDisposable
    {
        private readonly CheatsView _cheatsView;
        private readonly AddItemToInventoryUseCase _addItemUC;
        private readonly ItemRollingService _itemRolling;
        private readonly IGameStateProvider _gameState;
        private readonly ISkillGemConfigProvider _gemConfig;
        private readonly SkillGemInventory _gemInventory;
        private readonly IRandomService _random;
        private readonly IPublisher<InventoryChangedDTO> _inventoryChangedPub;
        private readonly IPublisher<ItemAddedDTO> _itemAddedPub;

        public CheatsPresenter(
            CheatsView cheatsView,
            AddItemToInventoryUseCase addItemUC,
            ItemRollingService itemRolling,
            IGameStateProvider gameState,
            ISkillGemConfigProvider gemConfig,
            SkillGemInventory gemInventory,
            IRandomService random,
            IPublisher<InventoryChangedDTO> inventoryChangedPub,
            IPublisher<ItemAddedDTO> itemAddedPub)
        {
            _cheatsView = cheatsView;
            _addItemUC = addItemUC;
            _itemRolling = itemRolling;
            _gameState = gameState;
            _gemConfig = gemConfig;
            _gemInventory = gemInventory;
            _random = random;
            _inventoryChangedPub = inventoryChangedPub;
            _itemAddedPub = itemAddedPub;
        }

        public void Start()
        {
            _cheatsView.OnGenerateItemClicked += HandleGenerateItem;
            _cheatsView.OnAddSkillGemClicked += HandleAddSkillGem;
            _cheatsView.OnAddRemovalOrbClicked += HandleAddRemovalOrb;
            _cheatsView.OnResetSaveClicked += HandleResetSave;

            Debug.Log("[CheatsPresenter] Initialized.");
        }

        private void HandleGenerateItem()
        {
            var item = _itemRolling.RollRandomItem();
            if (item == null)
            {
                _cheatsView.SetFeedback("No item definitions available.");
                return;
            }

            var inventory = _gameState.Inventory;
            if (!_addItemUC.Execute(inventory, item))
            {
                _cheatsView.SetFeedback("Inventory is full!");
                return;
            }

            _itemAddedPub.Publish(new ItemAddedDTO(item.Uid, item.Definition.Id));
            _inventoryChangedPub.Publish(new InventoryChangedDTO());

            var def = item.Definition;
            string rarityTag = def.Rarity != Rarity.Normal ? $" [{def.Rarity}]" : "";
            _cheatsView.SetFeedback($"Added: {def.Name}{rarityTag}\n+{item.RolledModifiers.Count} modifiers");
            Debug.Log($"[CheatsPresenter] Generated item: {def.Name} ({def.Slot}) with {item.RolledModifiers.Count} mods");
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

        private void HandleAddRemovalOrb()
        {
            _gemInventory.AddRemovalCurrency(5);
            _cheatsView.SetFeedback($"Added 5 Removal Orbs\nTotal: {_gemInventory.RemovalCurrencyCount}");
            Debug.Log($"[CheatsPresenter] Added 5 Removal Orbs. Total: {_gemInventory.RemovalCurrencyCount}");
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
            _cheatsView.OnAddSkillGemClicked -= HandleAddSkillGem;
            _cheatsView.OnAddRemovalOrbClicked -= HandleAddRemovalOrb;
            _cheatsView.OnResetSaveClicked -= HandleResetSave;
        }
    }
}
