using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Domain.Items;
using Game.Presentation.UI.Base;
using Game.Presentation.UI.DragDrop;
using Game.Presentation.UI.Tooltip;

namespace Game.Presentation.UI.MainScreen
{
    public sealed class EquipmentTabView : LayoutView
    {
        private VisualElement _equipmentSlots;
        private VisualElement _inventoryGrid;
        private Label _countLabel;

        public event Action<string, EquipmentSlotType> OnItemDroppedOnSlot;
        public event Action<EquipmentSlotType> OnSlotClicked;
        public event Action<string> OnItemClicked;

        private static readonly EquipmentSlotType[] AllSlots =
        {
            EquipmentSlotType.Weapon,
            EquipmentSlotType.Helmet,
            EquipmentSlotType.BodyArmor,
            EquipmentSlotType.Gloves,
            EquipmentSlotType.Boots
        };

        private static readonly Color[] SlotPlaceholderColors =
        {
            new(0.45f, 0.30f, 0.20f, 0.6f),
            new(0.30f, 0.35f, 0.45f, 0.6f),
            new(0.35f, 0.40f, 0.30f, 0.6f),
            new(0.40f, 0.30f, 0.35f, 0.6f),
            new(0.30f, 0.30f, 0.40f, 0.6f),
        };

        protected override void OnBind()
        {
            _equipmentSlots = Q("equipment-slots");
            _inventoryGrid = Q("inventory-grid");
            _countLabel = Q<Label>("inventory-count");
        }

        public VisualElement EquipmentSlotsContainer => _equipmentSlots;
        public VisualElement InventoryGridContainer => _inventoryGrid;

        public void RenderEquipment(IReadOnlyDictionary<EquipmentSlotType, ItemInstance> equipped)
        {
            _equipmentSlots.Clear();

            for (int i = 0; i < AllSlots.Length; i++)
            {
                var slotType = AllSlots[i];
                var slot = new VisualElement();
                slot.AddToClassList("equipment-slot");
                slot.userData = slotType;

                var slotLabel = new Label(FormatSlotName(slotType));
                slotLabel.AddToClassList("equipment-slot__label");

                equipped.TryGetValue(slotType, out var item);

                var icon = new VisualElement();
                icon.AddToClassList("item-icon");
                if (item != null)
                {
                    icon.AddToClassList("item-icon--filled");
                    icon.AddToClassList($"item-icon--{RarityClass(item.Definition.Rarity)}");
                }
                else
                {
                    icon.style.backgroundColor = SlotPlaceholderColors[i];
                }

                var itemLabel = new Label(item != null ? item.Definition.Name : "Empty");
                itemLabel.AddToClassList("item-label");
                if (item != null)
                    itemLabel.AddToClassList(RarityClass(item.Definition.Rarity));
                else
                    itemLabel.AddToClassList("item-label--empty");

                slot.Add(slotLabel);
                slot.Add(icon);
                slot.Add(itemLabel);

                var capturedSlot = slotType;
                var capturedItem = item;
                slot.RegisterCallback<ClickEvent>(_ => OnSlotClicked?.Invoke(capturedSlot));

                slot.RegisterCallback<PointerEnterEvent>(_ =>
                {
                    if (capturedItem != null)
                        ItemTooltip.Show(slot, capturedItem, Root);
                });
                slot.RegisterCallback<PointerLeaveEvent>(_ => ItemTooltip.Hide());

                _equipmentSlots.Add(slot);
            }
        }

        public void RenderInventory(IReadOnlyList<ItemInstance> items, int capacity)
        {
            _inventoryGrid.Clear();

            foreach (var item in items)
            {
                var slot = CreateItemSlot(item);
                _inventoryGrid.Add(slot);
            }

            int emptySlots = capacity - items.Count;
            for (int i = 0; i < emptySlots; i++)
                _inventoryGrid.Add(CreateEmptySlot());

            _countLabel.text = $"{items.Count} / {capacity}";
        }

        public void RaiseItemDroppedOnSlot(string itemUid, EquipmentSlotType slot) =>
            OnItemDroppedOnSlot?.Invoke(itemUid, slot);

        private VisualElement CreateItemSlot(ItemInstance item)
        {
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot");
            slot.userData = item;

            var icon = new VisualElement();
            icon.AddToClassList("item-icon");
            icon.AddToClassList("item-icon--small");
            icon.AddToClassList("item-icon--filled");
            icon.AddToClassList($"item-icon--{RarityClass(item.Definition.Rarity)}");
            slot.Add(icon);

            var label = new Label(item.Definition.Name);
            label.AddToClassList("item-label");
            label.AddToClassList(RarityClass(item.Definition.Rarity));
            slot.Add(label);

            slot.RegisterCallback<ClickEvent>(_ => OnItemClicked?.Invoke(item.Uid));

            slot.RegisterCallback<PointerEnterEvent>(_ =>
                ItemTooltip.Show(slot, item, Root));
            slot.RegisterCallback<PointerLeaveEvent>(_ => ItemTooltip.Hide());

            if (item.Definition.Slot != EquipmentSlotType.None)
            {
                var manipulator = new ItemDragManipulator(
                    (uid, slotType) => RaiseItemDroppedOnSlot(uid, slotType));
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

        private static string FormatSlotName(EquipmentSlotType slot) => slot switch
        {
            EquipmentSlotType.BodyArmor => "Body",
            _ => slot.ToString()
        };

        private static string RarityClass(Rarity r) => r switch
        {
            Rarity.Magic => "rarity-magic",
            Rarity.Rare => "rarity-rare",
            Rarity.Unique => "rarity-unique",
            _ => "rarity-normal"
        };
    }
}
