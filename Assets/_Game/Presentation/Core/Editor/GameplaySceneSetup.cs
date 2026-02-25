#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Presentation.Core.Bootstrap;
using Game.Presentation.UI.Base;
using Game.Presentation.UI.MainScreen;
using Game.Presentation.UI.Cheats;
using Game.Presentation.Combat;
using Game.Presentation.Combat.Rendering;

namespace Game.Presentation.Core.Editor
{
    public static class GameplaySceneSetup
    {
        private const string ScenePath = "Assets/Scenes/Gameplay.unity";
        private const string PanelSettingsPath = "Assets/_Game/Presentation/UI/Settings/GamePanelSettings.asset";

        private static readonly ViewDefinition[] AllViews =
        {
            new("MainScreen", "Assets/_Game/Presentation/UI/MainScreen/MainScreenView.uxml", typeof(MainScreenView), 0, true),
            new("CharacterTab", "Assets/_Game/Presentation/UI/MainScreen/CharacterTabView.uxml", typeof(CharacterTabView), 5, true),
            new("EquipmentTab", "Assets/_Game/Presentation/UI/MainScreen/EquipmentTabView.uxml", typeof(EquipmentTabView), 5, false),
            new("Cheats", "Assets/_Game/Presentation/UI/Cheats/CheatsView.uxml", typeof(CheatsView), 100, true),
        };

        [MenuItem("Idle Exile/Setup/Add Missing Views to Scene", false, 201)]
        public static void AddMissingViewsToScene()
        {
            int added = 0;
            foreach (var v in AllViews)
            {
                if (Object.FindFirstObjectByType(v.ViewType) != null)
                    continue;

                CreateViewObject(v.Name, v.UxmlPath, v.ViewType, v.SortOrder, v.VisibleOnStart);
                added++;
                Debug.Log($"[Idle Exile] Added missing view: [View] {v.Name}");
            }

            if (added == 0)
                Debug.Log("[Idle Exile] All views are already present in the scene.");
            else
            {
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                Debug.Log($"[Idle Exile] Added {added} missing view(s). Save the scene (Ctrl+S).");
            }
        }

        [MenuItem("Idle Exile/Setup/Create Gameplay Scene", false, 200)]
        public static void CreateGameplayScene()
        {
            if (System.IO.File.Exists(ScenePath))
            {
                if (!EditorUtility.DisplayDialog("Scene Exists",
                    $"Scene already exists at {ScenePath}. Overwrite?", "Yes", "Cancel"))
                    return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateLifetimeScope();
            CreateCombatObjects();
            foreach (var v in AllViews)
                CreateViewObject(v.Name, v.UxmlPath, v.ViewType, v.SortOrder, v.VisibleOnStart);

            EnsureDirectoryExists("Assets/Scenes");
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);

            var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            foreach (var s in buildScenes)
            {
                if (s.path == ScenePath) { found = true; break; }
            }
            if (!found)
            {
                buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            Debug.Log($"[Idle Exile] Gameplay scene created at {ScenePath} and added to Build Settings.");
        }

        private static void CreateCamera()
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.07f, 0.09f);
            cam.rect = new Rect(0f, 0f, 1f / 3f, 1f);
            cameraGo.tag = "MainCamera";
        }

        private static void CreateLifetimeScope()
        {
            var go = new GameObject("[GameplayLifetimeScope]");
            go.AddComponent<GameplayLifetimeScope>();
        }

        private static void CreateCombatObjects()
        {
            var combatRoot = new GameObject("[Combat]");

            var bridgeGo = new GameObject("CombatBridge");
            bridgeGo.transform.SetParent(combatRoot.transform);
            bridgeGo.AddComponent<CombatBridge>();

            var rendererGo = new GameObject("CombatRenderer");
            rendererGo.transform.SetParent(combatRoot.transform);
            rendererGo.AddComponent<CombatRenderer>();

            var poolGo = new GameObject("DamageNumberPool");
            poolGo.transform.SetParent(combatRoot.transform);
            poolGo.AddComponent<DamageNumberPool>();

            var camCtrlGo = new GameObject("CombatCameraController");
            camCtrlGo.transform.SetParent(combatRoot.transform);
            camCtrlGo.AddComponent<CombatCameraController>();
        }

        private static void CreateViewObject(string name, string uxmlPath, System.Type viewType,
            int sortOrder = 0, bool visibleOnStart = true)
        {
            var go = new GameObject($"[View] {name}");
            var uiDoc = go.AddComponent<UIDocument>();

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings != null)
                uiDoc.panelSettings = panelSettings;
            else
                Debug.LogWarning($"[Idle Exile] PanelSettings not found at {PanelSettingsPath}. Run 'Idle Exile > Setup > Create Panel Settings' first.");

            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (uxml != null)
                uiDoc.visualTreeAsset = uxml;

            uiDoc.sortingOrder = sortOrder;

            go.AddComponent(viewType);

            var view = go.GetComponent<LayoutView>();
            if (view != null)
            {
                var serializedObj = new SerializedObject(view);
                var visibleProp = serializedObj.FindProperty("_visibleOnStart");
                if (visibleProp != null)
                    visibleProp.boolValue = visibleOnStart;

                var sortProp = serializedObj.FindProperty("_sortOrder");
                if (sortProp != null)
                    sortProp.intValue = sortOrder;

                serializedObj.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Replace("\\", "/").Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private readonly struct ViewDefinition
        {
            public readonly string Name;
            public readonly string UxmlPath;
            public readonly System.Type ViewType;
            public readonly int SortOrder;
            public readonly bool VisibleOnStart;

            public ViewDefinition(string name, string uxmlPath, System.Type viewType, int sortOrder, bool visibleOnStart)
            {
                Name = name;
                UxmlPath = uxmlPath;
                ViewType = viewType;
                SortOrder = sortOrder;
                VisibleOnStart = visibleOnStart;
            }
        }
    }
}
#endif
