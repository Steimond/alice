using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Alice.UI
{
    /// <summary>
    /// The main menu's look and words (GDD 0.35): font, background, logo, cursor and every caption as a
    /// LocalizedString in the UI table. The menu builds itself in code from this, like the map and the rest menu.
    /// </summary>
    [CreateAssetMenu(menuName = "Alice/Menu Skin", fileName = "MenuSkin")]
    public sealed class MenuSkin : ScriptableObject
    {
        [Serializable]
        public sealed class Words
        {
            public LocalizedString Play = new LocalizedString();
            public LocalizedString Settings = new LocalizedString();
            public LocalizedString Quit = new LocalizedString();
            public LocalizedString Back = new LocalizedString();
            public LocalizedString SlotsTitle = new LocalizedString();
            [Tooltip("Smart string with {0}: the slot number.")]
            public LocalizedString Slot = new LocalizedString();
            public LocalizedString EmptySlot = new LocalizedString();
            public LocalizedString NewGame = new LocalizedString();
            public LocalizedString Continue = new LocalizedString();
            public LocalizedString Delete = new LocalizedString();
            public LocalizedString DeleteQuestion = new LocalizedString();
            public LocalizedString Yes = new LocalizedString();
            public LocalizedString No = new LocalizedString();
            [Tooltip("Smart string with {0}: hours, {1}: minutes.")]
            public LocalizedString PlayTime = new LocalizedString();
            [Tooltip("Smart string with {0}: decrees.")]
            public LocalizedString Decrees = new LocalizedString();
            [Tooltip("Smart string with {0}: abilities, {1}: trinkets.")]
            public LocalizedString Finds = new LocalizedString();
            public LocalizedString UnknownPlace = new LocalizedString();
            public LocalizedString SettingsTitle = new LocalizedString();
            public LocalizedString Language = new LocalizedString();
            public LocalizedString LanguageSystem = new LocalizedString();
            public LocalizedString Screen = new LocalizedString();
            public LocalizedString FullScreen = new LocalizedString();
            public LocalizedString Windowed = new LocalizedString();
            public LocalizedString WindowScale = new LocalizedString();
            public LocalizedString VSync = new LocalizedString();
            public LocalizedString MasterVolume = new LocalizedString();
            public LocalizedString MusicVolume = new LocalizedString();
            public LocalizedString EffectsVolume = new LocalizedString();
            public LocalizedString ScreenShake = new LocalizedString();
            public LocalizedString On = new LocalizedString();
            public LocalizedString Off = new LocalizedString();
            public LocalizedString Hints = new LocalizedString();
        }

        [SerializeField] private Font _font;
        [Tooltip("Full-screen picture, drawn at twice its pixel size (320 × 180 fills 640 × 360).")]
        [SerializeField] private Sprite _background;
        [SerializeField] private Sprite _logo;
        [SerializeField] private Sprite _cursor;
        [Tooltip("Scene loaded when a slot is chosen: Alice and the game systems.")]
        [SerializeField] private string _gameScene = "Bootstrap";
        [Tooltip("Names the zone a slot was saved in.")]
        [SerializeField] private Alice.World.MapAtlas _atlas;
        [SerializeField] private Words _words = new Words();

        public Font Font
        {
            get => _font;
            set => _font = value;
        }

        public Sprite Background
        {
            get => _background;
            set => _background = value;
        }

        public Sprite Logo
        {
            get => _logo;
            set => _logo = value;
        }

        public Sprite Cursor
        {
            get => _cursor;
            set => _cursor = value;
        }

        public string GameScene
        {
            get => _gameScene;
            set => _gameScene = value;
        }

        public Alice.World.MapAtlas Atlas
        {
            get => _atlas;
            set => _atlas = value;
        }

        public Words Text => _words;
    }
}
