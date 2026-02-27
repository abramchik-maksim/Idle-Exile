using System;
using System.Collections.Generic;
using MessagePipe;
using VContainer.Unity;
using Game.Application.Debug;
using Game.Application.Inventory;
using Game.Application.Ports;
using Game.Domain.DTOs.Debug;
using Game.Domain.DTOs.Inventory;
using Game.Domain.Items;
using Game.Domain.Stats;
using Game.Presentation.UI.Cheats;
using UnityEngine;

namespace Game.Presentation.UI.Presenters
{
    public sealed class CheatsPresenter : IStartable, IDisposable
    {
        private readonly CheatsView _cheatsView;
        private readonly SendTestMessageUseCase _sendTestUC;
        private readonly AddItemToInventoryUseCase _addItemUC;
        private readonly IGameStateProvider _gameState;
        private readonly IConfigProvider _config;
        private readonly IRandomService _random;
        private readonly ISubscriber<TestMessageDTO> _testMessageSub;
        private readonly IPublisher<InventoryChangedDTO> _inventoryChangedPub;
        private readonly IPublisher<ItemAddedDTO> _itemAddedPub;

        private readonly List<IDisposable> _subscriptions = new();
        private int _counter;

        public CheatsPresenter(
            CheatsView cheatsView,
            SendTestMessageUseCase sendTestUC,
            AddItemToInventoryUseCase addItemUC,
            IGameStateProvider gameState,
            IConfigProvider config,
            IRandomService random,
            ISubscriber<TestMessageDTO> testMessageSub,
            IPublisher<InventoryChangedDTO> inventoryChangedPub,
            IPublisher<ItemAddedDTO> itemAddedPub)
        {
            _cheatsView = cheatsView;
            _sendTestUC = sendTestUC;
            _addItemUC = addItemUC;
            _gameState = gameState;
            _config = config;
            _random = random;
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
            var allItems = _config.GetAllItems();
            if (allItems.Count == 0)
            {
                _cheatsView.SetFeedback("No item definitions available.");
                return;
            }

            var def = allItems[_random.Next(0, allItems.Count)];
            var mods = RollModifiers(def);
            var item = new ItemInstance(def, mods);

            var inventory = _gameState.Inventory;
            if (!_addItemUC.Execute(inventory, item))
            {
                _cheatsView.SetFeedback("Inventory is full!");
                return;
            }

            _itemAddedPub.Publish(new ItemAddedDTO(item.Uid, def.Id));
            _inventoryChangedPub.Publish(new InventoryChangedDTO());

            string rarityTag = def.Rarity != Rarity.Normal ? $" [{def.Rarity}]" : "";
            _cheatsView.SetFeedback($"Added: {def.Name}{rarityTag}\n+{mods.Count} modifiers");
            Debug.Log($"[CheatsPresenter] Generated item: {def.Name} ({def.Slot}) with {mods.Count} mods");
        }

        private List<Modifier> RollModifiers(ItemDefinition def)
        {
            var mods = new List<Modifier>();
            int count = def.Rarity switch
            {
                Rarity.Normal => 0,
                Rarity.Magic => _random.Next(1, 3),
                Rarity.Rare => _random.Next(3, 6),
                _ => 1
            };

            var pool = ModifierRollingConfig.RollableStats;

            for (int i = 0; i < count; i++)
            {
                var stat = pool[_random.Next(0, pool.Length)];
                var (modType, min, max) = ModifierRollingConfig.GetRange(stat);
                float value = _random.NextFloat(min, max);
                mods.Add(new Modifier(stat, modType, value, "rolled"));
            }

            return mods;
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
