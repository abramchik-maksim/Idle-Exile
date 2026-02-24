using UnityEngine.UIElements;
using Game.Domain.Items;

namespace Game.Presentation.UI.DragDrop
{
    public static class EquipmentSlotDropZone
    {
        public static void Setup(VisualElement slotElement, EquipmentSlotType slotType)
        {
            slotElement.userData = slotType;
            slotElement.AddToClassList("equipment-slot");
        }
    }
}
