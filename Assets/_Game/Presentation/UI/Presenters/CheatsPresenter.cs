using System;
using System.Collections.Generic;
using MessagePipe;
using VContainer.Unity;
using Game.Application.Debug;
using Game.Application.Inventory;
using Game.Application.Loot;
using Game.Application.Ports;
using Game.Domain.DTOs.Debug;
using Game.Domain.DTOs.Inventory;
using Game.Domain.Items;
using Game.Presentation.UI.Cheats;
using UnityEngine;

namespace Game.Presentation.UI.Presenters
{
    public sealed class CheatsPresenter : IStartable, IDisposable
    {
        private readonly CheatsView _cheatsView;
        private readonly SendTestMessageUseCase _sendTestUC;
        private readonly AddItemToInventoryUseCase _addItemUC;
        private readonly ItemRollingService _itemRolling;
        private readonly IGameStateProvider _gameState;
        private readonly ISubscriber<TestMessageDTO> _testMessageSub;
        private readonly IPublisher<InventoryChangedDTO> _inventoryChangedPub;
        private readonly IPublisher<ItemAddedDTO> _itemAddedPub;

        private readonly List<IDisposable> _subscriptions = new();
        private int _counter;

        public CheatsPresenter(
            CheatsView cheatsView,
            SendTestMessageUseCase sendTestUC,
            AddItemToInventoryUseCase addItemUC,
            ItemRollingService itemRolling,
            IGameStateProvider gameState,
            ISubscriber<TestMessageDTO> testMessageSub,
            IPublisher<InventoryChangedDTO> inventoryChangedPub,
            IPublisher<ItemAddedDTO> itemAddedPub)
        {
            _cheatsView = cheatsView;
            _sendTestUC = sendTestUC;
            _addItemUC = addItemUC;
            _itemRolling = itemRolling;
            _gameState = gameState;
            _testMessageSub = testMessageSub;
            _inventoryChangedPub = inventoryChangedPub;
            _itemAddedPub = itemAddedPub;
        }

        public void Start()
        {
            _cheatsView.OnSendTestClicked += HandleSendTest;
            _cheatsView.OnGenerateItemClicked += HandleGenerateItem;
            _cheatsView.OnResetSaveClicked += HandleResetSave;

            _subscriptions.Add(
                _testMessageSub.Subscribe(dto =>
                {
                    Debug.Log($"[CheatsPresenter] Received TestMessageDTO: {dto.Message}");
                    _cheatsView.SetFeedback($"Received: {dto.Message}");
                }));

            Debug.Log("[CheatsPresenter] Initialized and listening.");
        }

        private void HandleSendTest()
        {
            _counter++;
            string msg = $"Test #{_counter} at {DateTime.Now:HH:mm:ss}";
            Debug.Log($"[CheatsPresenter] Publishing: {msg}");
            _sendTestUC.Execute(msg);
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
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();
        }
    }
}
