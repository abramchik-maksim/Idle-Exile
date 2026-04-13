#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Presentation.Core.Editor
{
    public static class MainMenuSceneSetup
    {
        [MenuItem("Idle Exile/Setup/Configure Build Settings (Boot/MainMenu/Gameplay)", false, 192)]
        public static void ConfigureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            AddIfExists(scenes, "Assets/Scenes/Boot.unity");
            AddIfExists(scenes, "Assets/Scenes/MainMenu.unity");
            AddIfExists(scenes, "Assets/Scenes/Gameplay.unity");

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Idle Exile] Build Settings configured: Boot -> MainMenu -> Gameplay");
        }

        private static void AddIfExists(List<EditorBuildSettingsScene> scenes, string path)
        {
            if (System.IO.File.Exists(path))
                scenes.Add(new EditorBuildSettingsScene(path, true));
        }
    }
}
#endif
