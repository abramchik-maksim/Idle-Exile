using System.Collections.Generic;
using UnityEngine.UIElements;
using Game.Domain.Items;
using Game.Presentation.UI.Base;

namespace Game.Presentation.UI.Inventory
{
    public sealed class InventoryView : LayoutView
    {
        private VisualElement _grid;
        private Label _countLabel;
        private Button _btnClose;

        public event System.Action OnCloseClicked;
        public event System.Action<string> OnItemClicked;

        protected override void OnBind()
        {
            _grid = Q("inventory-grid");
            _countLabel = Q<Label>("inventory-count");
            _btnClose = Q<Button>("btn-close");

            _btnClose.clicked += () => OnCloseClicked?.Invoke();
        }

        public void Render(IReadOnlyList<ItemInstance> items, int capacity)
        {
            _grid.Clear();

            foreach (var item in items)
            {
                var slot = CreateSlot(item);
                _grid.Add(slot);
            }

            int emptySlots = capacity - items.Count;
            for (int i = 0; i < emptySlots; i++)
                _grid.Add(CreateEmptySlot());

            _countLabel.text = $"{items.Count} / {capacity}";
        }

        private VisualElement CreateSlot(ItemInstance item)
        {
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot");

            var label = new Label(item.Definition.Name);
            label.AddToClassList("item-label");
            label.AddToClassList(RarityClass(item.Definition.Rarity));
            slot.Add(label);

            slot.RegisterCallback<ClickEvent>(_ => OnItemClicked?.Invoke(item.Uid));

            return slot;
        }

        private static VisualElement CreateEmptySlot()
        {
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot");
            return slot;
        }

        private static string RarityClass(Rarity r) => r switch
        {
            Rarity.Magic => "rarity-magic",
            Rarity.Rare => "rarity-rare",
            Rarity.Unique => "rarity-unique",
            _ => "rarity-normal"
        };
    }
}
