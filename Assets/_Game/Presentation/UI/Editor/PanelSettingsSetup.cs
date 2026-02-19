#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Presentation.UI.Editor
{
    public static class PanelSettingsSetup
    {
        private const string AssetPath = "Assets/_Game/Presentation/UI/Settings/GamePanelSettings.asset";

        [MenuItem("Idle Exile/Setup/Create Panel Settings", false, 100)]
        public static void CreatePanelSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetPath);
            if (existing != null)
            {
                Debug.Log($"PanelSettings already exists at {AssetPath}");
                Selection.activeObject = existing;
                return;
            }

            var dir = System.IO.Path.GetDirectoryName(AssetPath);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                var parts = dir.Replace("\\", "/").Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }

            var ps = ScriptableObject.CreateInstance<PanelSettings>();

            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            ps.sortingOrder = 0;

            AssetDatabase.CreateAsset(ps, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = ps;
            Debug.Log($"PanelSettings created at {AssetPath}. Assign it to UIDocuments on View prefabs.");
        }
    }
}
#endif
