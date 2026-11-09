using System;
using System.IO;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Alice.Core
{
    /// <summary>
    /// The player's settings (GDD 0.35): language, screen, sound levels and camera shake. Kept apart from the save
    /// slots in settings.json in persistentDataPath, applied when the game starts and whenever the settings
    /// screen changes them. Sound levels are kept for the mixer to come; the master level already drives the
    /// listener.
    /// </summary>
    [Serializable]
    public sealed class GameSettings
    {
        public const int MinWindowScale = 2;
        public const int MaxWindowScale = 4;
        public const int MaxVolume = 10;
        private const string FileName = "settings.json";

        /// <summary>Locale code ("ru", "en"); empty follows the system language.</summary>
        public string Language = string.Empty;
        public bool FullScreen = true;
        /// <summary>Window size as a whole multiple of 640 × 360.</summary>
        public int WindowScale = 3;
        public bool VSync = true;
        public int MasterVolume = MaxVolume;
        public int MusicVolume = 8;
        public int EffectsVolume = MaxVolume;
        public bool ScreenShake = true;

        private static GameSettings _current;

        /// <summary>Raised after the settings change and are applied, so open screens redraw.</summary>
        public static event Action Changed;

        public static GameSettings Current => _current ??= Load();

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyAtStart()
        {
            Current.Apply();
        }

        public static GameSettings Load()
        {
            GameSettings settings = null;
            try
            {
                if (File.Exists(FilePath))
                {
                    settings = JsonUtility.FromJson<GameSettings>(File.ReadAllText(FilePath));
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Settings could not be read, defaults are used: {exception.Message}");
            }

            settings ??= new GameSettings();
            settings.Clamp();
            return settings;
        }

        /// <summary>Applies and keeps the settings; the settings screen calls this after every change.</summary>
        public void Commit()
        {
            Clamp();
            Apply();
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(this, true));
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"Settings could not be written: {exception.Message}");
            }

            Changed?.Invoke();
        }

        public void Apply()
        {
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            AudioListener.volume = MasterVolume / (float)MaxVolume;
            ApplyLanguage();
            // In the Editor the Game view decides its own size.
            if (!Application.isEditor)
            {
                if (FullScreen)
                {
                    Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
                }
                else
                {
                    Screen.SetResolution(WorldMetrics.ReferenceWidth * WindowScale, WorldMetrics.ReferenceHeight * WindowScale,
                        FullScreenMode.Windowed);
                }
            }
        }

        private void Clamp()
        {
            WindowScale = Mathf.Clamp(WindowScale, MinWindowScale, MaxWindowScale);
            MasterVolume = Mathf.Clamp(MasterVolume, 0, MaxVolume);
            MusicVolume = Mathf.Clamp(MusicVolume, 0, MaxVolume);
            EffectsVolume = Mathf.Clamp(EffectsVolume, 0, MaxVolume);
            Language ??= string.Empty;
        }

        private void ApplyLanguage()
        {
            if (string.IsNullOrEmpty(Language))
            {
                return;
            }

            string code = Language;
            void Select()
            {
                Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
                if (locale != null && LocalizationSettings.SelectedLocale != locale)
                {
                    LocalizationSettings.SelectedLocale = locale;
                }
            }

            var initialization = LocalizationSettings.InitializationOperation;
            if (initialization.IsDone)
            {
                Select();
            }
            else
            {
                initialization.Completed += _ => Select();
            }
        }
    }
}
