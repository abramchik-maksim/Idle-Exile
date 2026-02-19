using System.Collections.Generic;
using UnityEngine.UIElements;
using Game.Domain.Items;
using Game.Domain.Stats;
using Game.Presentation.UI.Base;

namespace Game.Presentation.UI.CharacterPanel
{
    public sealed class CharacterPanelView : LayoutView
    {
        private VisualElement _equipmentSlots;
        private VisualElement _statsContainer;
        private Button _btnClose;

        public event System.Action OnCloseClicked;
        public event System.Action<EquipmentSlotType> OnSlotClicked;

        protected override void OnBind()
        {
            _equipmentSlots = Q("equipment-slots");
            _statsContainer = Q("stats-container");
            _btnClose = Q<Button>("btn-close");

            _btnClose.clicked += () => OnCloseClicked?.Invoke();
        }

        public void RenderEquipment(IReadOnlyDictionary<EquipmentSlotType, ItemInstance> equipped)
        {
            _equipmentSlots.Clear();

            var allSlots = new[]
            {
                EquipmentSlotType.Weapon, EquipmentSlotType.Helmet, EquipmentSlotType.BodyArmor,
                EquipmentSlotType.Gloves, EquipmentSlotType.Boots
            };

            foreach (var slotType in allSlots)
            {
                var slot = new VisualElement();
                slot.AddToClassList("inventory-slot");

                string labelText = equipped.TryGetValue(slotType, out var item)
                    ? item.Definition.Name
                    : slotType.ToString();

                var label = new Label(labelText);
                label.AddToClassList("item-label");
                if (item != null)
                    label.AddToClassList($"rarity-{item.Definition.Rarity.ToString().ToLowerInvariant()}");
                else
                    label.style.color = new StyleColor(new UnityEngine.Color(0.5f, 0.5f, 0.5f));

                slot.Add(label);
                slot.RegisterCallback<ClickEvent>(_ => OnSlotClicked?.Invoke(slotType));
                _equipmentSlots.Add(slot);
            }
        }

        public void RenderStats(IReadOnlyDictionary<StatType, float> stats)
        {
            _statsContainer.Clear();

            foreach (var kvp in stats)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };

                var nameLabel = new Label(kvp.Key.ToString());
                nameLabel.AddToClassList("stat-label");

                var valueLabel = new Label(FormatStatValue(kvp.Key, kvp.Value));
                valueLabel.AddToClassList("stat-value");

                row.Add(nameLabel);
                row.Add(valueLabel);
                _statsContainer.Add(row);
            }
        }

        private static string FormatStatValue(StatType stat, float val) => stat switch
        {
            StatType.CriticalChance => $"{val * 100f:F1}%",
            StatType.CriticalMultiplier => $"x{val:F2}",
            StatType.AttackSpeed => $"{val:F2}/s",
            _ => $"{val:F0}"
        };
    }
}
