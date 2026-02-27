using UnityEditor;
using UnityEngine;

namespace Game.Presentation.Core.Editor
{
    public static class SaveDataTools
    {
        [MenuItem("Idle Exile/Tools/Reset Save Data", priority = 300)]
        public static void ResetSaveData()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Reset Save Data",
                    "Cannot reset while in Play Mode.\nExit Play Mode first, then try again.",
                    "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Reset Save Data",
                "This will permanently delete ALL save data:\n" +
                "- Player progress (tier, map, battle)\n" +
                "- Inventory & equipped items\n\n" +
                "Are you sure?",
                "Delete All", "Cancel");

            if (!confirmed) return;

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            Debug.Log("[SaveDataTools] All save data has been reset.");
        }
    }
}
