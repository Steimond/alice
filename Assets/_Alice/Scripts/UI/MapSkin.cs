using System;
using Alice.World;
using UnityEngine;
using UnityEngine.Localization;

namespace Alice.UI
{
    /// <summary>
    /// Words and optional pictures of the map screen: the font, the legend label of every icon, the hint line and
    /// the mark caption, all from the localization table. An icon without a sprite is drawn by MapGlyphs.
    /// </summary>
    [CreateAssetMenu(menuName = "Alice/Map Skin", fileName = "MapSkin")]
    public sealed class MapSkin : ScriptableObject
    {
        [Serializable]
        public sealed class Legend
        {
            public MapIconKind Kind;
            public LocalizedString Label = new LocalizedString();
            [Tooltip("Optional picture instead of the drawn glyph.")]
            public Sprite Icon;
        }

        [SerializeField] private Font _font;
        [SerializeField] private Legend[] _legend = new Legend[0];
        [SerializeField] private LocalizedString _hints = new LocalizedString();
        [SerializeField] private LocalizedString _markCaption = new LocalizedString();
        [SerializeField] private LocalizedString _unknownZone = new LocalizedString();

        public Font Font
        {
            get => _font;
            set => _font = value;
        }

        public Legend[] Entries
        {
            get => _legend;
            set => _legend = value;
        }

        public LocalizedString Hints
        {
            get => _hints;
            set => _hints = value;
        }

        public LocalizedString MarkCaption
        {
            get => _markCaption;
            set => _markCaption = value;
        }

        public LocalizedString UnknownZone
        {
            get => _unknownZone;
            set => _unknownZone = value;
        }

        public Legend Find(MapIconKind kind)
        {
            return Array.Find(_legend, entry => entry != null && entry.Kind == kind);
        }
    }
}
