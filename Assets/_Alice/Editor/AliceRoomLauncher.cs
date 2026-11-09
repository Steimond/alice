using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Alice.Editor
{
    /// <summary>
    /// One click into any room or test scene, past the main menu: opens the scene and plays it once
    /// (<see cref="AlicePlayStart.PlayHere"/>). The menu rule itself stays on.
    /// </summary>
    public sealed class AliceRoomLauncher : EditorWindow
    {
        private const string ScenesFolder = "Assets/_Alice/Scenes";
        private const float ButtonWidth = 120f;

        // The menu is what this window skips, and Bootstrap is joined by every room on its own.
        private static readonly HashSet<string> Skipped = new HashSet<string> { "MainMenu", "Bootstrap" };

        private Vector2 _scroll;

        [MenuItem("Alice/Play Room...", false, 2)]
        private static void Open()
        {
            GetWindow<AliceRoomLauncher>("Play Room");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Opens the scene and enters Play without the main menu, this one time. "
                + "F5 does the same for the scene already open.", MessageType.None);

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                foreach (IGrouping<string, string> group in FindScenes())
                {
                    EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);
                    DrawButtons(group.ToList());
                    EditorGUILayout.Space(4f);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawButtons(List<string> paths)
        {
            int perRow = Mathf.Max(1, Mathf.FloorToInt((position.width - 8f) / (ButtonWidth + 4f)));
            for (int start = 0; start < paths.Count; start += perRow)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    foreach (string path in paths.Skip(start).Take(perRow))
                    {
                        if (GUILayout.Button(Path.GetFileNameWithoutExtension(path), GUILayout.Width(ButtonWidth)))
                        {
                            // Deferred: switching scenes inside OnGUI breaks the layout of this very frame.
                            EditorApplication.delayCall += () => Play(path);
                        }
                    }
                }
            }
        }

        private static IEnumerable<IGrouping<string, string>> FindScenes()
        {
            return AssetDatabase.FindAssets("t:Scene", new[] { ScenesFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !Skipped.Contains(Path.GetFileNameWithoutExtension(path)))
                .OrderBy(path => path)
                .GroupBy(GroupOf);
        }

        private static string GroupOf(string path)
        {
            string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            return folder == ScenesFolder ? "Test scenes" : Path.GetFileName(folder);
        }

        private static void Play(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode
                || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            AlicePlayStart.PlayHere();
        }
    }
}
