using System;
using System.Collections.Generic;
using MessagePipe;
using VContainer.Unity;
using Game.Application.Inventory;
using Game.Application.Ports;
using Game.Domain.Items;
using Game.Domain.DTOs.Inventory;
using Game.Domain.DTOs.Stats;
using Game.Presentation.UI.MainScreen;
using Game.Presentation.UI.Services;

namespace Game.Presentation.UI.Presenters
{
    public sealed class EquipmentPresenter : IStartable, IDisposable
    {
        private readonly EquipmentTabView _view;
        private readonly IGameStateProvider _gameState;
        private readonly IIconProvider _iconProvider;
        private readonly EquipItemUseCase _equipItemUseCase;
        private readonly UnequipItemUseCase _unequipItemUseCase;
        private readonly IPublisher<ItemEquippedDTO> _itemEquippedPub;
        private readonly IPublisher<ItemUnequippedDTO> _itemUnequippedPub;
        private readonly IPublisher<InventoryChangedDTO> _inventoryChangedPub;
        private readonly IPublisher<HeroStatsChangedDTO> _statsChangedPub;
        private readonly ISubscriber<InventoryChangedDTO> _inventoryChangedSub;
        private readonly ISubscriber<ItemEquippedDTO> _itemEquippedSub;
        private readonly ISubscriber<ItemUnequippedDTO> _itemUnequippedSub;

        private readonly List<IDisposable> _subscriptions = new();

        public EquipmentPresenter(
            EquipmentTabView view,
            IGameStateProvider gameState,
            IIconProvider iconProvider,
            EquipItemUseCase equipItemUseCase,
            UnequipItemUseCase unequipItemUseCase,
            IPublisher<ItemEquippedDTO> itemEquippedPub,
            IPublisher<ItemUnequippedDTO> itemUnequippedPub,
            IPublisher<InventoryChangedDTO> inventoryChangedPub,
            IPublisher<HeroStatsChangedDTO> statsChangedPub,
            ISubscriber<InventoryChangedDTO> inventoryChangedSub,
            ISubscriber<ItemEquippedDTO> itemEquippedSub,
            ISubscriber<ItemUnequippedDTO> itemUnequippedSub)
        {
            _view = view;
            _gameState = gameState;
            _iconProvider = iconProvider;
            _equipItemUseCase = equipItemUseCase;
            _unequipItemUseCase = unequipItemUseCase;
            _itemEquippedPub = itemEquippedPub;
            _itemUnequippedPub = itemUnequippedPub;
            _inventoryChangedPub = inventoryChangedPub;
            _statsChangedPub = statsChangedPub;
            _inventoryChangedSub = inventoryChangedSub;
            _itemEquippedSub = itemEquippedSub;
            _itemUnequippedSub = itemUnequippedSub;
        }

        public void Start()
        {
            _view.SetIconProvider(_iconProvider);
            _view.OnItemDroppedOnSlot += HandleEquipItem;
            _view.OnItemRightClicked += HandleEquipItemByUid;
            _view.OnSlotRightClicked += HandleUnequipSlot;
            _view.OnSlotDraggedOff += HandleUnequipSlot;
            _view.OnItemCompareRequested += HandleCompare;
            _view.OnDeleteAllClicked += HandleDeleteAll;
            _view.OnItemSellDropped += HandleSellItem;
            _view.OnEquipmentSellDropped += HandleSellEquipped;

            _subscriptions.Add(
                _inventoryChangedSub.Subscribe(_ => RefreshAll()));

            _subscriptions.Add(
                _itemEquippedSub.Subscribe(_ => RefreshAll()));

            _subscriptions.Add(
                _itemUnequippedSub.Subscribe(_ => RefreshAll()));

            RefreshAll();

            UnityEngine.Debug.Log("[EquipmentPresenter] Initialized.");
        }

        private void HandleEquipItem(string itemUid, EquipmentSlotType targetSlot)
        {
            EquipByUid(itemUid, targetSlot);
        }

        private void HandleEquipItemByUid(string itemUid)
        {
            EquipByUid(itemUid);
        }

        private void EquipByUid(string itemUid, EquipmentSlotType targetSlot = EquipmentSlotType.None)
        {
            var inventory = _gameState.Inventory;
            var hero = _gameState.Hero;

            var result = _equipItemUseCase.Execute(inventory, hero, itemUid, targetSlot);
            if (!result.Success)
                return;

            _itemEquippedPub.Publish(new ItemEquippedDTO(itemUid, result.Slot.Value));
            _inventoryChangedPub.Publish(new InventoryChangedDTO());

            if (result.FinalStats != null)
                _statsChangedPub.Publish(new HeroStatsChangedDTO(result.FinalStats));
        }

        private void HandleUnequipSlot(EquipmentSlotType slotType)
        {
            var inventory = _gameState.Inventory;
            var hero = _gameState.Hero;

            if (!inventory.Equipped.ContainsKey(slotType))
                return;

            var result = _unequipItemUseCase.Execute(inventory, hero, slotType);
            if (!result.Success)
                return;

            _itemUnequippedPub.Publish(new ItemUnequippedDTO(result.Slot.Value));
            _inventoryChangedPub.Publish(new InventoryChangedDTO());

            if (result.FinalStats != null)
                _statsChangedPub.Publish(new HeroStatsChangedDTO(result.FinalStats));
        }

        private void HandleDeleteAll()
        {
            var inventory = _gameState.Inventory;
            if (inventory.Items.Count == 0) return;

            inventory.ClearItems();
            _inventoryChangedPub.Publish(new InventoryChangedDTO());
            UnityEngine.Debug.Log("[EquipmentPresenter] All inventory items deleted.");
        }

        private void HandleSellItem(string uid)
        {
            var inventory = _gameState.Inventory;
            if (!inventory.Remove(uid)) return;

            _inventoryChangedPub.Publish(new InventoryChangedDTO());
            UnityEngine.Debug.Log($"[EquipmentPresenter] Item sold: {uid}");
        }

        private void HandleSellEquipped(EquipmentSlotType slotType)
        {
            var inventory = _gameState.Inventory;
            var hero = _gameState.Hero;

            if (!inventory.Equipped.ContainsKey(slotType)) return;

            var result = _unequipItemUseCase.Execute(inventory, hero, slotType);
            if (!result.Success) return;

            inventory.Remove(result.UnequippedItem.Uid);

            _itemUnequippedPub.Publish(new ItemUnequippedDTO(result.Slot.Value));
            _inventoryChangedPub.Publish(new InventoryChangedDTO());

            if (result.FinalStats != null)
                _statsChangedPub.Publish(new HeroStatsChangedDTO(result.FinalStats));

            UnityEngine.Debug.Log($"[EquipmentPresenter] Equipped item sold from slot: {slotType}");
        }

        private void HandleCompare(ItemInstance item)
        {
            if (item == null) return;

            var inventory = _gameState.Inventory;
            var equipped = inventory.GetEquippedFor(item.Definition.Slot);

            _view.ShowItemComparison(item, equipped);
        }

        private void RefreshAll()
        {
            var inventory = _gameState.Inventory;
            _view.RenderEquipment(inventory.Equipped);
            _view.RenderInventory(inventory.Items, inventory.Capacity);
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();
        }
    }
}
