using UnityEditor;
using UnityEditor.SceneManagement;

namespace Alice.Editor
{
    /// <summary>
    /// Play in the Editor always starts from the main menu, whatever scene is open (owner's decision, GDD 0.35),
    /// as the built game does. For checking one room on its own the switch Alice → Play From Main Menu turns it
    /// off for this machine; it stays on by default.
    /// </summary>
    [InitializeOnLoad]
    public static class AlicePlayStart
    {
        private const string MenuPath = "Alice/Play From Main Menu";
        private const string PrefKey = "Alice.PlayFromMainMenu";

        static AlicePlayStart()
        {
            // Set once the asset database is ready, and again right before every Play, so a domain reload in between
            // never leaves the open scene as the start.
            EditorApplication.delayCall += Apply;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                {
                    Apply();
                }
            };
        }

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set
            {
                EditorPrefs.SetBool(PrefKey, value);
                Apply();
            }
        }

        [MenuItem(MenuPath, false, 0)]
        private static void Toggle()
        {
            Enabled = !Enabled;
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleCheck()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        public static void Apply()
        {
            EditorSceneManager.playModeStartScene = Enabled && !IsTestRun()
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(AliceMainMenuSetup.ScenePath)
                : null;
        }

        // The test runner enters Play from its own InitTestScene; starting from the menu instead would leave the
        // PlayMode tests waiting for a scene that never loads.
        private static bool IsTestRun()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.StartsWith("InitTestScene");
        }
    }
}
