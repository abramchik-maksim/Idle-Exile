using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Domain.Items;
using Game.Presentation.UI.Base;
using Game.Presentation.UI.DragDrop;
using Game.Presentation.UI.Services;
using Game.Presentation.UI.Tooltip;

namespace Game.Presentation.UI.MainScreen
{
    public sealed class EquipmentTabView : LayoutView
    {
        private VisualElement _columnLeft;
        private VisualElement _columnRight;
        private VisualElement _inventoryGrid;
        private Label _countLabel;
        private IIconProvider _iconProvider;

        public event Action<string, EquipmentSlotType> OnItemDroppedOnSlot;
        public event Action<string> OnItemRightClicked;
        public event Action<EquipmentSlotType> OnSlotRightClicked;
        public event Action<EquipmentSlotType> OnSlotDraggedOff;
        public event Action<ItemInstance> OnItemCompareRequested;

        private VisualElement _compareAnchor;

        private static readonly EquipmentSlotType[] LeftColumnSlots =
        {
            EquipmentSlotType.Helmet,
            EquipmentSlotType.Amulet,
            EquipmentSlotType.Belt,
            EquipmentSlotType.Ring1,
            EquipmentSlotType.MainHand,
        };

        private static readonly EquipmentSlotType[] RightColumnSlots =
        {
            EquipmentSlotType.BodyArmor,
            EquipmentSlotType.Gloves,
            EquipmentSlotType.Boots,
            EquipmentSlotType.Ring2,
            EquipmentSlotType.OffHand,
        };

        protected override void OnBind()
        {
            _columnLeft = Q("equipment-column-left");
            _columnRight = Q("equipment-column-right");
            _inventoryGrid = Q("inventory-grid");
            _countLabel = Q<Label>("inventory-count");
        }

        public void SetIconProvider(IIconProvider provider) => _iconProvider = provider;

        public VisualElement EquipmentSlotsContainer => _columnLeft?.parent;
        public VisualElement InventoryGridContainer => _inventoryGrid;

        public void RenderEquipment(IReadOnlyDictionary<EquipmentSlotType, ItemInstance> equipped)
        {
            _columnLeft.Clear();
            _columnRight.Clear();

            bool offHandBlocked = equipped.TryGetValue(EquipmentSlotType.MainHand, out var mainHandItem)
                                  && mainHandItem.Definition.Handedness == Handedness.TwoHanded;

            foreach (var slotType in LeftColumnSlots)
            {
                equipped.TryGetValue(slotType, out var item);
                _columnLeft.Add(CreateEquipmentSlot(slotType, item, offHandBlocked));
            }

            foreach (var slotType in RightColumnSlots)
            {
                equipped.TryGetValue(slotType, out var item);
                _columnRight.Add(CreateEquipmentSlot(slotType, item, offHandBlocked));
            }
        }

        public void RenderInventory(IReadOnlyList<ItemInstance> items, int capacity)
        {
            _inventoryGrid.Clear();

            foreach (var item in items)
                _inventoryGrid.Add(CreateItemSlot(item));

            int emptySlots = capacity - items.Count;
            for (int i = 0; i < emptySlots; i++)
                _inventoryGrid.Add(CreateEmptySlot());

            _countLabel.text = $"{items.Count} / {capacity}";
        }

        public void RaiseItemDroppedOnSlot(string itemUid, EquipmentSlotType slot) =>
            OnItemDroppedOnSlot?.Invoke(itemUid, slot);

        public void ShowItemComparison(ItemInstance item, ItemInstance equipped)
        {
            if (_compareAnchor == null) return;
            ItemTooltip.ShowComparison(_compareAnchor, item, equipped, Root);
        }

        private VisualElement CreateEquipmentSlot(EquipmentSlotType slotType, ItemInstance item,
            bool offHandBlocked)
        {
            var slot = new VisualElement();
            slot.AddToClassList("equipment-slot");
            slot.userData = slotType;

            bool isBlocked = slotType == EquipmentSlotType.OffHand && offHandBlocked;
            if (isBlocked)
                slot.AddToClassList("equipment-slot--blocked");

            var slotLabel = new Label(isBlocked ? "Off Hand\n(blocked)" : FormatSlotName(slotType));
            slotLabel.AddToClassList("equipment-slot__label");
            slot.Add(slotLabel);

            if (item != null)
            {
                ApplyRarityStyle(slot, item.Definition.Rarity);

                var icon = CreateIconElement(item, false);
                slot.Add(icon);
                LoadIconAsync(icon, item).Forget();
            }

            if (isBlocked) return slot;

            var capturedSlot = slotType;
            var capturedItem = item;

            slot.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button == 1 && capturedItem != null)
                {
                    OnSlotRightClicked?.Invoke(capturedSlot);
                    evt.StopPropagation();
                }
            });

            slot.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (capturedItem != null)
                    ItemTooltip.Show(slot, capturedItem, Root);
            });
            slot.RegisterCallback<PointerLeaveEvent>(_ => ItemTooltip.Hide());

            if (capturedItem != null)
            {
                var dragManip = new ItemDragManipulator(
                    explicitItem: capturedItem,
                    onDragReleased: () => OnSlotDraggedOff?.Invoke(capturedSlot));
                slot.AddManipulator(dragManip);
            }

            return slot;
        }

        private VisualElement CreateItemSlot(ItemInstance item)
        {
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot");
            slot.userData = item;

            ApplyRarityStyle(slot, item.Definition.Rarity);

            var icon = CreateIconElement(item, true);
            slot.Add(icon);
            LoadIconAsync(icon, item).Forget();

            slot.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button == 1)
                {
                    ItemTooltip.Hide();
                    OnItemRightClicked?.Invoke(item.Uid);
                    evt.StopPropagation();
                }
            });

            slot.RegisterCallback<PointerEnterEvent>(_ =>
                ItemTooltip.Show(slot, item, Root));
            slot.RegisterCallback<PointerLeaveEvent>(_ => ItemTooltip.Hide());

            if (item.Definition.Slot != EquipmentSlotType.None)
            {
                var capturedSlot = slot;
                var manipulator = new ItemDragManipulator(
                    onDroppedOnSlot: (uid, slotType) => RaiseItemDroppedOnSlot(uid, slotType),
                    onClicked: clickedItem =>
                    {
                        ItemTooltip.Hide();
                        _compareAnchor = capturedSlot;
                        OnItemCompareRequested?.Invoke(clickedItem);
                    });
                slot.AddManipulator(manipulator);
            }

            return slot;
        }

        private static VisualElement CreateEmptySlot()
        {
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot");
            return slot;
        }

        private static VisualElement CreateIconElement(ItemInstance item, bool small)
        {
            var icon = new VisualElement { name = "item-icon" };
            icon.AddToClassList("item-icon");
            if (small)
                icon.AddToClassList("item-icon--small");

            var rarityKey = RarityKey(item.Definition.Rarity);
            icon.AddToClassList($"item-icon--placeholder-{rarityKey}");
            return icon;
        }

        private async UniTaskVoid LoadIconAsync(VisualElement iconElement, ItemInstance item)
        {
            if (_iconProvider == null || string.IsNullOrEmpty(item.Definition.IconAddress))
                return;

            var sprite = await _iconProvider.LoadIconAsync(item.Definition.IconAddress);
            if (sprite == null) return;

            iconElement.style.backgroundImage = new StyleBackground(sprite);
            iconElement.RemoveFromClassList($"item-icon--placeholder-{RarityKey(item.Definition.Rarity)}");
        }

        private static void ApplyRarityStyle(VisualElement slot, Rarity rarity)
        {
            var key = RarityKey(rarity);
            slot.AddToClassList($"slot-bg--{key}");
            slot.AddToClassList($"slot-border--{key}");

            if (rarity is Rarity.Rare or Rarity.Unique)
            {
                var glow = new VisualElement();
                glow.AddToClassList($"slot-glow--{key}");
                glow.pickingMode = PickingMode.Ignore;
                slot.Add(glow);
            }
        }

        private static string FormatSlotName(EquipmentSlotType slot) => slot switch
        {
            EquipmentSlotType.Helmet => "Head",
            EquipmentSlotType.BodyArmor => "Body",
            EquipmentSlotType.Gloves => "Gloves",
            EquipmentSlotType.Boots => "Boots",
            EquipmentSlotType.Amulet => "Amulet",
            EquipmentSlotType.Belt => "Belt",
            EquipmentSlotType.Ring1 => "Ring",
            EquipmentSlotType.Ring2 => "Ring",
            EquipmentSlotType.MainHand => "Main Hand",
            EquipmentSlotType.OffHand => "Off Hand",
            _ => slot.ToString()
        };

        public static string RarityKey(Rarity r) => r switch
        {
            Rarity.Magic => "magic",
            Rarity.Rare => "rare",
            Rarity.Unique => "unique",
            _ => "normal"
        };
    }
}
