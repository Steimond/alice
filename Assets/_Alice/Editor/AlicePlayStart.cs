using UnityEditor;
using UnityEditor.SceneManagement;

namespace Alice.Editor
{
    /// <summary>
    /// Play in the Editor always starts from the main menu, whatever scene is open (owner's decision, GDD 0.35),
    /// as the built game does. Two ways past it for checking one room: Alice → Play This Scene (F5) starts the open
    /// scene once and leaves the rule on, and Alice → Play From Main Menu turns the rule off for this machine.
    /// </summary>
    [InitializeOnLoad]
    public static class AlicePlayStart
    {
        private const string MenuPath = "Alice/Play From Main Menu";
        private const string PlayHerePath = "Alice/Play This Scene _F5";
        private const string PrefKey = "Alice.PlayFromMainMenu";
        // SessionState, not EditorPrefs: the one-off skip survives the domain reload on entering Play but never
        // outlives the Editor session, so a crash mid-Play cannot leave the menu switched off for good.
        private const string HereOnceKey = "Alice.PlayHereOnce";

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
                else if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(HereOnceKey, false))
                {
                    // Play has already picked its start scene; the next Play goes through the menu again.
                    SessionState.EraseBool(HereOnceKey);
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

        /// <summary>Enters Play in the open scene, skipping the main menu this one time.</summary>
        [MenuItem(PlayHerePath, false, 1)]
        public static void PlayHere()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            SessionState.SetBool(HereOnceKey, true);
            Apply();
            EditorApplication.isPlaying = true;
        }

        [MenuItem(PlayHerePath, true)]
        private static bool PlayHereCheck()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        public static void Apply()
        {
            bool fromMenu = Enabled && !SessionState.GetBool(HereOnceKey, false) && !IsTestRun();
            EditorSceneManager.playModeStartScene = fromMenu
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
