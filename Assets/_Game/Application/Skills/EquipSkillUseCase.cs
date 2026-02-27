using Game.Domain.Items;
using Game.Domain.Skills;
using InventoryModel = Game.Domain.Inventory.Inventory;

namespace Game.Application.Skills
{
    public sealed class EquipSkillUseCase
    {
        public EquipSkillResult Execute(
            SkillCollection skills,
            SkillLoadout loadout,
            InventoryModel inventory,
            string skillUid,
            int targetSlot = -1)
        {
            var skill = skills.Find(skillUid);
            if (skill == null)
                return EquipSkillResult.Fail;

            if (skill.Definition.Category == SkillCategory.Main)
            {
                if (!ValidateWeaponRequirement(skill.Definition, inventory))
                    return EquipSkillResult.Fail;

                if (!loadout.TryEquipMain(skill, out _))
                    return EquipSkillResult.Fail;

                return new EquipSkillResult(true, SkillLoadout.MainSlotIndex);
            }

            if (skill.Definition.Category == SkillCategory.Utility)
            {
                int slot = targetSlot >= SkillLoadout.FirstUtilitySlotIndex
                    ? targetSlot
                    : FindFirstEmptyUtilitySlot(loadout);

                if (slot < SkillLoadout.FirstUtilitySlotIndex)
                    return EquipSkillResult.Fail;

                if (!loadout.TryEquipUtility(skill, slot, out _))
                    return EquipSkillResult.Fail;

                return new EquipSkillResult(true, slot);
            }

            return EquipSkillResult.Fail;
        }

        private static bool ValidateWeaponRequirement(SkillDefinition def, InventoryModel inventory)
        {
            if (def.RequiredWeapon == WeaponType.None)
                return true;

            if (!inventory.Equipped.TryGetValue(EquipmentSlotType.MainHand, out var weapon))
                return false;

            return weapon.Definition.WeaponType == def.RequiredWeapon;
        }

        private static int FindFirstEmptyUtilitySlot(SkillLoadout loadout)
        {
            for (int i = SkillLoadout.FirstUtilitySlotIndex; i < SkillLoadout.TotalSlots; i++)
            {
                if (loadout.IsSlotEmpty(i))
                    return i;
            }
            return -1;
        }
    }

    public sealed class EquipSkillResult
    {
        public static readonly EquipSkillResult Fail = new(false, -1);

        public bool Success { get; }
        public int SlotIndex { get; }

        public EquipSkillResult(bool success, int slotIndex)
        {
            Success = success;
            SlotIndex = slotIndex;
        }
    }
}
