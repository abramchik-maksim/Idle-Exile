using System.IO;
using UnityEngine;

namespace Game.Infrastructure.Repositories
{
    internal static class FileSavePaths
    {
        public static string SaveRoot => Path.Combine(UnityEngine.Application.persistentDataPath, "saves");

        public static string SlotDirectory(int slotIndex) => Path.Combine(SaveRoot, $"slot_{slotIndex}");

        public static string MetadataPath(int slotIndex) => Path.Combine(SlotDirectory(slotIndex), "metadata.json");
        public static string ProgressPath(int slotIndex) => Path.Combine(SlotDirectory(slotIndex), "progress.json");
        public static string InventoryPath(int slotIndex) => Path.Combine(SlotDirectory(slotIndex), "inventory.json");
        public static string TreeTalentsPath(int slotIndex) => Path.Combine(SlotDirectory(slotIndex), "tree_talents.json");
    }
}
