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
    /// The in-game map (GDD 0.33): its words in the UI localization table, the map skin, the atlas baked from the
    /// room scenes, and the map components on Alice. Idempotent: words already in the table, zones already in the
    /// atlas (with their names and tints) and the components' settings are kept. Baking the atlas again after rooms
    /// change is a separate menu, also called by the room builders.
    /// </summary>
    public static class AliceMapSetup
    {
        private const string Root = "Assets/_Alice";
        private const string TableName = "UI";
        private const string TableFolder = Root + "/Data/Localization";
        private const string SkinPath = Root + "/Data/UI/MapSkin.asset";
        private const string AtlasPath = Root + "/Data/World/MapAtlas.asset";
        private const string FontPath = Root + "/Art/Fonts/FusionPixel/FusionPixel12-Latin.otf";
        private const string AlicePrefabPath = Root + "/Prefabs/Characters/Alice.prefab";

        // Zones known so far: stable ID, name key, tint on the map. New zones are added here or in the atlas.
        private static readonly (string Id, Color Tint)[] Zones =
        {
            ("B01", new Color(0.2f, 0.33f, 0.36f))
        };

        private static readonly (string Key, string Ru, string En)[] Words =
        {
            ("map.zone.B01", "Нижние покои", "Lower Quarters"),
            ("map.zone.unknown", "Неизвестные места", "Uncharted Parts"),
            ("map.mark", "Отметка:", "Mark:"),
            ("map.hints", "Стик — курсор · Действие — отметка · Плечи — вид отметки · Прыжок — масштаб · Карта — закрыть",
                "Stick — cursor · Interact — mark · Shoulders — mark type · Jump — zoom · Map — close"),
            ("map.legend.Alice", "Алиса", "Alice"),
            ("map.legend.Rest", "Кресло отдыха", "Resting chair"),
            ("map.legend.ShortcutDoor", "Короткий путь", "Shortcut door"),
            ("map.legend.Lift", "Лифт", "Lift"),
            ("map.legend.GateFan", "Нужен веер", "Needs the fan"),
            ("map.legend.GateMushroom", "Нужен гриб", "Needs the mushroom"),
            ("map.legend.GateCheshire", "Нужен Чеширский шаг", "Needs the Cheshire Step"),
            ("map.legend.GateClock", "Нужны часы", "Needs the watch"),
            ("map.legend.LostDecrees", "Потерянные указы", "Lost decrees"),
            ("map.legend.MarkPin", "Булавка", "Pin"),
            ("map.legend.MarkCross", "Крестик", "Cross"),
            ("map.legend.MarkButton", "Пуговица", "Button"),
            ("map.legend.MarkKnot", "Узелок", "Knot")
        };

        [MenuItem("Alice/Prototype/Set Up Map")]
        public static void SetUpFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Map: leave Play mode first.");
                return;
            }

            SetUp();
        }

        [MenuItem("Alice/Level/Bake World Map")]
        public static void BakeFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Map: leave Play mode first.");
                return;
            }

            BakeAtlas();
        }

        public static void SetUp()
        {
            EnsureWords();
            MapSkin skin = EnsureSkin();
            MapAtlas atlas = BakeAtlas();
            EnsureOnAlice(atlas, skin);
            AssetDatabase.SaveAssets();
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
                    if (table.GetEntry(key) != null)
                    {
                        continue;
                    }

                    // Russian is the source language; every other locale starts from English.
                    table.AddEntry(key, code == "ru" ? ru : en);
                    EditorUtility.SetDirty(table);
                }
            }

            EditorUtility.SetDirty(collection.SharedData);
        }

        private static MapSkin EnsureSkin()
        {
            AliceProjectSetup.EnsureFolder(Root + "/Data/UI");
            var skin = AssetDatabase.LoadAssetAtPath<MapSkin>(SkinPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<MapSkin>();
                AssetDatabase.CreateAsset(skin, SkinPath);
            }

            if (skin.Font == null)
            {
                skin.Font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            }

            List<MapSkin.Legend> legend = (skin.Entries ?? new MapSkin.Legend[0]).Where(entry => entry != null).ToList();
            foreach (MapIconKind kind in System.Enum.GetValues(typeof(MapIconKind)))
            {
                if (legend.All(entry => entry.Kind != kind))
                {
                    legend.Add(new MapSkin.Legend { Kind = kind, Label = new LocalizedString(TableName, "map.legend." + kind) });
                }
            }

            skin.Entries = legend.ToArray();
            if (skin.Hints.IsEmpty)
            {
                skin.Hints = new LocalizedString(TableName, "map.hints");
            }

            if (skin.MarkCaption.IsEmpty)
            {
                skin.MarkCaption = new LocalizedString(TableName, "map.mark");
            }

            if (skin.UnknownZone.IsEmpty)
            {
                skin.UnknownZone = new LocalizedString(TableName, "map.zone.unknown");
            }

            EditorUtility.SetDirty(skin);
            return skin;
        }

        /// <summary>
        /// Rebuilds the atlas's rooms and icons from every room scene with painted geometry; zones keep their
        /// names and tints. Scenes that were not open are opened alongside and closed without saving.
        /// </summary>
        public static MapAtlas BakeAtlas()
        {
            AliceProjectSetup.EnsureFolder(Root + "/Data/World");
            var atlas = AssetDatabase.LoadAssetAtPath<MapAtlas>(AtlasPath);
            if (atlas == null)
            {
                atlas = ScriptableObject.CreateInstance<MapAtlas>();
                AssetDatabase.CreateAsset(atlas, AtlasPath);
            }

            var rooms = new List<MapAtlas.RoomEntry>();
            var icons = new List<MapAtlas.IconEntry>();
            string folder = AliceLevelRoomTools.NewRoomsFolder;
            string[] paths = AssetDatabase.IsValidFolder(folder)
                ? AssetDatabase.FindAssets("t:Scene", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray()
                : new string[0];
            foreach (string path in paths)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool wasOpen = scene.IsValid() && scene.isLoaded;
                if (!wasOpen)
                {
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }

                try
                {
                    Collect(scene, rooms, icons);
                }
                finally
                {
                    if (!wasOpen)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }

            atlas.Rooms = rooms.ToArray();
            atlas.Icons = icons.ToArray();
            List<MapAtlas.ZoneEntry> zones = (atlas.Zones ?? new MapAtlas.ZoneEntry[0]).Where(zone => zone != null).ToList();
            foreach ((string id, Color tint) in Zones)
            {
                if (zones.All(zone => zone.Id != id))
                {
                    zones.Add(new MapAtlas.ZoneEntry { Id = id, Tint = tint, Name = new LocalizedString(TableName, "map.zone." + id) });
                }
            }

            atlas.Zones = zones.ToArray();
            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();
            Debug.Log($"Map: baked {rooms.Count} rooms and {icons.Count} icons into {AtlasPath}.");
            return atlas;
        }

        private static void Collect(Scene scene, List<MapAtlas.RoomEntry> rooms, List<MapAtlas.IconEntry> icons)
        {
            Room room = AliceLevelRoomTools.FindRoom(scene);
            AliceLevelWorld.RoomShape shape = AliceLevelWorld.Shape(scene);
            if (room == null || shape == null)
            {
                return;
            }

            var cells = new char[shape.Rect.width * shape.Rect.height];
            for (int y = 0; y < shape.Rect.height; y++)
            {
                for (int x = 0; x < shape.Rect.width; x++)
                {
                    cells[y * shape.Rect.width + x] = shape.Solid[x, y] ? MapAtlas.Rock
                        : shape.Hazard[x, y] ? MapAtlas.Hazard
                        : shape.OneWay[x, y] ? MapAtlas.Catwalk
                        : MapAtlas.Air;
                }
            }

            rooms.Add(new MapAtlas.RoomEntry
            {
                Id = room.RoomId,
                ZoneId = room.ZoneId,
                Secret = room.Secret,
                Origin = room.WorldPosition,
                Rect = shape.Rect,
                Cells = new string(cells)
            });

            Vector2 origin = room.WorldPosition;
            void Add(MapIconKind kind, Vector3 position, float lift, string flag)
            {
                icons.Add(new MapAtlas.IconEntry
                {
                    Kind = kind,
                    RoomId = room.RoomId,
                    Position = (Vector2)position + origin + new Vector2(0f, lift),
                    Flag = flag ?? string.Empty
                });
            }

            GameObject[] roots = scene.GetRootGameObjects();
            foreach (RestPoint rest in roots.SelectMany(root => root.GetComponentsInChildren<RestPoint>(true)))
            {
                Add(MapIconKind.Rest, rest.transform.position, 1f, null);
            }

            foreach (PermanentDoor door in roots.SelectMany(root => root.GetComponentsInChildren<PermanentDoor>(true)))
            {
                Add(MapIconKind.ShortcutDoor, door.transform.position, 2f, door.Flag);
            }

            foreach (LittleDoor door in roots.SelectMany(root => root.GetComponentsInChildren<LittleDoor>(true)))
            {
                Add(MapIconKind.GateMushroom, door.transform.position, 0.5f, door.Flag);
            }

            foreach (CheshireBridge bridge in roots.SelectMany(root => root.GetComponentsInChildren<CheshireBridge>(true)))
            {
                Add(MapIconKind.GateCheshire, bridge.transform.position, 1f, null);
            }

            foreach (ClockGuillotine guillotine in roots.SelectMany(root => root.GetComponentsInChildren<ClockGuillotine>(true)))
            {
                Add(MapIconKind.GateClock, guillotine.transform.position, 2f, null);
            }

            foreach (MapPoint point in roots.SelectMany(root => root.GetComponentsInChildren<MapPoint>(true)))
            {
                Add(point.Kind, point.transform.position, 0f, point.Flag);
            }
        }

        private static void EnsureOnAlice(MapAtlas atlas, MapSkin skin)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(AlicePrefabPath);
            try
            {
                if (!root.TryGetComponent(out MapProgress progress))
                {
                    progress = root.AddComponent<MapProgress>();
                }

                if (progress.Atlas == null)
                {
                    progress.Atlas = atlas;
                }

                if (!root.TryGetComponent(out MapScreen screen))
                {
                    screen = root.AddComponent<MapScreen>();
                }

                if (screen.Skin == null)
                {
                    screen.Skin = skin;
                }

                PrefabUtility.SaveAsPrefabAsset(root, AlicePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
