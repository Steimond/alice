using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Alice.Editor
{
    /// <summary>Creates the project foundation without overwriting existing content.</summary>
    public static class AliceProjectSetup
    {
        private const string Root = "Assets/_Alice";
        private const string BootstrapPath = Root + "/Scenes/Bootstrap.unity";
        private const string PrototypePath = Root + "/Scenes/Prototype_Traversal.unity";
        private const string InputPath = Root + "/Settings/AliceInput.inputactions";

        [MenuItem("Alice/Project/Configure Foundation")]
        public static void ConfigureFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            // Unity cannot create scenes additively while an untitled scene is open.
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (string.IsNullOrEmpty(scene.path) && !EditorSceneManager.SaveScene(scene))
                {
                    return;
                }
            }

            Configure();
        }

        // Batch entry point: -executeMethod Alice.Editor.AliceProjectSetup.ConfigureBatch
        public static void ConfigureBatch()
        {
            // A fresh batch Editor starts with an untitled scene. Use an existing
            // saved scene as the anchor; this process has no user's unsaved work.
            string anchor = File.Exists(BootstrapPath)
                ? BootstrapPath
                : "Assets/Scenes/SampleScene.unity";
            if (!File.Exists(anchor))
            {
                throw new FileNotFoundException("A saved scene is required for foundation setup.", anchor);
            }

            EditorSceneManager.OpenScene(anchor, OpenSceneMode.Single);
            Configure();
            Debug.Log("ALICE_SETUP_COMPLETE");
        }

        private static void Configure()
        {
            string[] folders =
            {
                "Art/Characters", "Art/Environments", "Art/UI", "Animations",
                "Audio/Music", "Audio/SFX", "Materials", "Prefabs/Characters",
                "Prefabs/Enemies", "Prefabs/World", "Scenes/Biomes", "Scenes/Tests",
                "Scripts/Core", "Scripts/Player", "Scripts/Combat", "Scripts/Abilities",
                "Scripts/World", "Scripts/Narrative", "Scripts/UI", "Scripts/Save",
                "Data/Biomes", "Data/Enemies", "Data/Abilities", "Data/Dialogue",
                "Settings", "Editor"
            };

            foreach (string folder in folders)
            {
                EnsureFolder(Root + "/" + folder);
            }

            EditorSettings.serializationMode = SerializationMode.ForceText;
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            UnityEditor.VersionControlSettings.mode = "Visible Meta Files";
            PlayerSettings.companyName = "Alice Project";
            PlayerSettings.productName = "Alice";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = false;

            CreateInputIfMissing();
            AddMissingGameplayActions();
            AliceLocalizationSetup.Configure();
            string currentInputPath = InputSystem.actions == null
                ? string.Empty
                : AssetDatabase.GetAssetPath(InputSystem.actions);
            if (string.IsNullOrEmpty(currentInputPath)
                || currentInputPath == "Assets/Settings/InputSystem_Actions.inputactions")
            {
                InputSystem.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            }

            SceneSetup[] previousScenes = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                CreateSceneIfMissing(BootstrapPath, false);
                CreateSceneIfMissing(PrototypePath, true);
            }
            finally
            {
                if (previousScenes.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousScenes);
                }
            }

            // Preserve any user build list. Replace only the untouched template list.
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            bool templateOnly = buildScenes.Length == 0 || Array.TrueForAll(
                buildScenes, scene => scene.path == "Assets/Scenes/SampleScene.unity");
            if (templateOnly)
            {
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(BootstrapPath, true),
                    new EditorBuildSettingsScene(PrototypePath, true)
                };
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Alice foundation configured. Gameplay implementation is the next milestone.");
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void CreateSceneIfMissing(string path, bool prototype)
        {
            if (File.Exists(path))
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Scene previousActiveScene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.045f, 0.07f);
            cameraObject.AddComponent<UniversalAdditionalCameraData>().SetRenderer(0);

            if (prototype)
            {
                new GameObject("World");
                GameObject gameplay = new GameObject("Gameplay");
                GameObject spawn = new GameObject("PlayerSpawn");
                spawn.transform.SetParent(gameplay.transform);
                new GameObject("UI");
            }
            else
            {
                new GameObject("Systems");
            }

            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new IOException("Could not save scene: " + path);
            }

            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousActiveScene);
            }

            EditorSceneManager.CloseScene(scene, true);
        }

        private static void CreateInputIfMissing()
        {
            if (File.Exists(InputPath))
            {
                return;
            }

            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            try
            {
                InputActionMap gameplay = asset.AddActionMap("Gameplay");
                InputAction move = gameplay.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
                move.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
                move.AddBinding("<Gamepad>/leftStick");
                move.AddBinding("<Gamepad>/dpad");

                AddButton(gameplay, "Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
                AddButton(gameplay, "Attack", "<Mouse>/leftButton", "<Gamepad>/buttonWest");
                AddButton(gameplay, "Evade", "<Keyboard>/leftShift", "<Gamepad>/buttonEast");
                AddButton(gameplay, "Parry", "<Keyboard>/f", "<Gamepad>/rightTrigger");
                AddButton(gameplay, "Interact", "<Keyboard>/e", "<Gamepad>/buttonNorth");
                AddButton(gameplay, "Heal", "<Keyboard>/q", "<Gamepad>/leftShoulder");
                AddButton(gameplay, "UseAbility", "<Mouse>/rightButton", "<Gamepad>/rightShoulder");
                AddButton(gameplay, "Map", "<Keyboard>/m", "<Gamepad>/select");
                AddButton(gameplay, "Pause", "<Keyboard>/escape", "<Gamepad>/start");

                InputActionMap ui = asset.AddActionMap("UI");
                InputAction navigate = ui.AddAction("Navigate", InputActionType.Value, expectedControlLayout: "Vector2");
                navigate.AddBinding("<Gamepad>/leftStick");
                navigate.AddBinding("<Gamepad>/dpad");
                navigate.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
                AddButton(ui, "Submit", "<Keyboard>/enter", "<Gamepad>/buttonSouth");
                AddButton(ui, "Cancel", "<Keyboard>/escape", "<Gamepad>/buttonEast");
                ui.AddAction("Point", InputActionType.PassThrough, "<Pointer>/position", expectedControlLayout: "Vector2");
                ui.AddAction("Click", InputActionType.PassThrough, "<Pointer>/press", expectedControlLayout: "Button");

                File.WriteAllText(InputPath, asset.ToJson() + Environment.NewLine);
                AssetDatabase.ImportAsset(InputPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        // Adds actions introduced after the asset was first created; existing bindings stay untouched.
        private static void AddMissingGameplayActions()
        {
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            InputActionMap gameplay = asset == null ? null : asset.FindActionMap("Gameplay");
            if (gameplay == null || gameplay.FindAction("Parry") != null)
            {
                return;
            }

            InputActionAsset copy = InputActionAsset.FromJson(asset.ToJson());
            try
            {
                AddButton(copy.FindActionMap("Gameplay"), "Parry", "<Keyboard>/f", "<Gamepad>/rightTrigger");
                File.WriteAllText(InputPath, copy.ToJson() + Environment.NewLine);
                AssetDatabase.ImportAsset(InputPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }

        private static void AddButton(InputActionMap map, string name, string keyboard, string gamepad)
        {
            InputAction action = map.AddAction(name, InputActionType.Button);
            action.AddBinding(keyboard);
            action.AddBinding(gamepad);
        }
    }
}
