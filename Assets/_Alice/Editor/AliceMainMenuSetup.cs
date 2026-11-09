using System.Collections.Generic;
using System.Linq;
using Alice.UI;
using Alice.World;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

namespace Alice.Editor
{
    /// <summary>
    /// Alice → Prototype → Set Up Main Menu (GDD 0.35): the menu's words in the UI table, the MenuSkin with the
    /// art from Art/UI/Menu, and the MainMenu scene, first in Build Settings. Idempotent: words already in the
    /// table and skin fields already set are kept; the scene is created only when missing.
    /// </summary>
    public static class AliceMainMenuSetup
    {
        public const string ScenePath = "Assets/_Alice/Scenes/MainMenu.unity";
        private const string Root = "Assets/_Alice";
        private const string TableName = "UI";
        private const string TableFolder = Root + "/Data/Localization";
        private const string SkinPath = Root + "/Data/UI/MenuSkin.asset";
        private const string ArtFolder = Root + "/Art/UI/Menu";
        private const string FontPath = Root + "/Art/Fonts/FusionPixel/FusionPixel12-Latin.otf";
        private const string AtlasPath = Root + "/Data/World/MapAtlas.asset";

        private static readonly (string Key, string Ru, string En)[] Words =
        {
            ("menu.play", "Играть", "Play"),
            ("menu.settings", "Настройки", "Settings"),
            ("menu.quit", "Выйти", "Quit"),
            ("menu.back", "Назад", "Back"),
            ("menu.slots", "Выбор сохранения", "Choose a save"),
            ("menu.slot", "Слот {0}", "Slot {0}"),
            ("menu.slot.empty", "Пусто — новая игра", "Empty — new game"),
            ("menu.newgame", "Новая игра", "New game"),
            ("menu.continue", "Продолжить", "Continue"),
            ("menu.delete", "Стереть", "Erase"),
            ("menu.delete.question", "Стереть слот {0}? Это нельзя отменить.", "Erase slot {0}? This cannot be undone."),
            ("menu.yes", "Да", "Yes"),
            ("menu.no", "Нет", "No"),
            ("menu.playtime", "{0} ч {1:00} мин", "{0} h {1:00} min"),
            ("menu.decrees", "Указов: {0}", "Decrees: {0}"),
            ("menu.finds", "Находок: {0}", "Finds: {0}"),
            ("menu.place.unknown", "Неизвестные места", "Uncharted parts"),
            ("menu.settings.title", "Настройки", "Settings"),
            ("menu.language", "Язык", "Language"),
            ("menu.language.system", "Как в системе", "As the system"),
            ("menu.screen", "Экран", "Display"),
            ("menu.fullscreen", "Полный экран", "Full screen"),
            ("menu.windowed", "Окно", "Window"),
            ("menu.windowscale", "Размер окна", "Window size"),
            ("menu.vsync", "Вертикальная синхронизация", "V-Sync"),
            ("menu.volume.master", "Общая громкость", "Master volume"),
            ("menu.volume.music", "Музыка", "Music"),
            ("menu.volume.effects", "Звуки", "Sound effects"),
            ("menu.shake", "Тряска камеры", "Camera shake"),
            ("menu.on", "Вкл.", "On"),
            ("menu.off", "Выкл.", "Off"),
            ("menu.hints", "Действие — выбрать · Отмена — назад · Влево и вправо — изменить",
                "Submit — choose · Cancel — back · Left and right — change")
        };

        [MenuItem("Alice/Prototype/Set Up Main Menu")]
        public static void SetUpFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Main menu: leave Play mode first.");
                return;
            }

            SetUp();
        }

        public static void SetUp()
        {
            EnsureWords();
            MenuSkin skin = EnsureSkin();
            EnsureScene(skin);
            EnsureBuildFirst();
            AlicePlayStart.Apply();
            AssetDatabase.SaveAssets();
            Debug.Log("Main menu: words, skin and scene are ready; the scene is first in Build Settings.");
        }

        private static void EnsureWords()
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(TableName)
                ?? LocalizationEditorSettings.CreateStringTableCollection(TableName, TableFolder);
            foreach (StringTable table in collection.StringTables)
            {
                string code = table.LocaleIdentifier.Code;
                foreach ((string key, string ru, string en) in Words)
                {
                    if (table.GetEntry(key) == null)
                    {
                        // Russian is the source language; every other locale starts from English.
                        table.AddEntry(key, code == "ru" ? ru : en);
                        EditorUtility.SetDirty(table);
                    }
                }
            }

            EditorUtility.SetDirty(collection.SharedData);
        }

        private static MenuSkin EnsureSkin()
        {
            AliceProjectSetup.EnsureFolder(Root + "/Data/UI");
            var skin = AssetDatabase.LoadAssetAtPath<MenuSkin>(SkinPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<MenuSkin>();
                AssetDatabase.CreateAsset(skin, SkinPath);
            }

            if (skin.Font == null)
            {
                skin.Font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            }

            if (skin.Background == null)
            {
                skin.Background = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/menu_background.png");
            }

            if (skin.Logo == null)
            {
                skin.Logo = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/menu_logo.png");
            }

            if (skin.Cursor == null)
            {
                skin.Cursor = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/menu_cursor.png");
            }

            if (skin.Atlas == null)
            {
                skin.Atlas = AssetDatabase.LoadAssetAtPath<MapAtlas>(AtlasPath);
            }


            MenuSkin.Words words = skin.Text;
            Word(ref words.Play, "menu.play");
            Word(ref words.Settings, "menu.settings");
            Word(ref words.Quit, "menu.quit");
            Word(ref words.Back, "menu.back");
            Word(ref words.SlotsTitle, "menu.slots");
            Word(ref words.Slot, "menu.slot");
            Word(ref words.EmptySlot, "menu.slot.empty");
            Word(ref words.NewGame, "menu.newgame");
            Word(ref words.Continue, "menu.continue");
            Word(ref words.Delete, "menu.delete");
            Word(ref words.DeleteQuestion, "menu.delete.question");
            Word(ref words.Yes, "menu.yes");
            Word(ref words.No, "menu.no");
            Word(ref words.PlayTime, "menu.playtime");
            Word(ref words.Decrees, "menu.decrees");
            Word(ref words.Finds, "menu.finds");
            Word(ref words.UnknownPlace, "menu.place.unknown");
            Word(ref words.SettingsTitle, "menu.settings.title");
            Word(ref words.Language, "menu.language");
            Word(ref words.LanguageSystem, "menu.language.system");
            Word(ref words.Screen, "menu.screen");
            Word(ref words.FullScreen, "menu.fullscreen");
            Word(ref words.Windowed, "menu.windowed");
            Word(ref words.WindowScale, "menu.windowscale");
            Word(ref words.VSync, "menu.vsync");
            Word(ref words.MasterVolume, "menu.volume.master");
            Word(ref words.MusicVolume, "menu.volume.music");
            Word(ref words.EffectsVolume, "menu.volume.effects");
            Word(ref words.ScreenShake, "menu.shake");
            Word(ref words.On, "menu.on");
            Word(ref words.Off, "menu.off");
            Word(ref words.Hints, "menu.hints");
            EditorUtility.SetDirty(skin);
            return skin;
        }

        private static void Word(ref LocalizedString text, string key)
        {
            if (text == null || text.IsEmpty)
            {
                text = new LocalizedString(TableName, key);
            }
        }

        private static void EnsureScene(MenuSkin skin)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                return;
            }

            Scene previous = SceneManager.GetActiveScene();
            string previousPath = previous.path;
            if (previous.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var menu = new GameObject("MainMenu").AddComponent<MainMenu>();
            menu.Skin = skin;
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!string.IsNullOrEmpty(previousPath))
            {
                EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
            }
        }

        private static void EnsureBuildFirst()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(entry => entry.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
