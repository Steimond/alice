using System.Collections.Generic;
using System.IO;
using System.Linq;
using Alice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alice.Editor
{
    /// <summary>
    /// Room-level helpers of the Level Palette: placing an object on the floor, giving it a fresh stable ID,
    /// checking a room for the mistakes that break travel and saves, and starting a new room scene.
    /// </summary>
    public static class AliceLevelRoomTools
    {
        public const string NewRoomsFolder = "Assets/_Alice/Scenes/Rooms";
        private const string CatalogPath = "Assets/_Alice/Data/World/Rooms.asset";
        private const string GameplayName = "Gameplay";
        private const string DecreeCachePrefab = "Assets/_Alice/Prefabs/World/DecreeCache.prefab";
        private const string DefaultRoomId = "P_ROOM";
        private const string UnsetMark = "TODO";

        // The stable-ID fields the palette fills in and checks for clashes: component, field, prefix.
        private static readonly (System.Type Type, string Field, string Prefix)[] IdFields =
        {
            (typeof(RoomDoorway), "_doorwayId", "D"),
            (typeof(PermanentDoor), "_doorId", "SD"),
            (typeof(RestPoint), "_restId", "R"),
            (typeof(LittleDoor), "_doorId", "LD")
        };

        public static Room FindRoom(Scene scene)
        {
            return scene.GetRootGameObjects().Select(root => root.GetComponentInChildren<Room>(true))
                .FirstOrDefault(room => room != null);
        }

        /// <summary>
        /// Puts a prefab instance with the bottom of its visible and solid parts on <paramref name="floor"/>, under
        /// Gameplay, and gives it fresh IDs. A clock marker binds itself to the nearest guillotine.
        /// </summary>
        public static GameObject Place(Scene scene, GameObject prefab, Vector2 floor)
        {
            Transform gameplay = AliceLevelGeometry.EnsureRoot(scene, GameplayName);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(instance, "Place " + prefab.name);
            instance.transform.SetParent(gameplay, false);
            instance.transform.position = new Vector3(floor.x, floor.y, 0f);
            float bottom = BottomOffset(instance);
            instance.transform.position = new Vector3(floor.x, floor.y - bottom, 0f);
            AssignIds(scene, instance);
            BindClockMarker(scene, instance);
            EditorSceneManager.MarkSceneDirty(scene);
            return instance;
        }

        // How far the lowest drawn or solid point sits below the pivot. Doorways have only a trigger, which then
        // counts, so their opening starts at the floor too.
        private static float BottomOffset(GameObject instance)
        {
            float pivot = instance.transform.position.y;
            var lows = new List<float>();
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is SpriteRenderer sprite && sprite.sprite == null)
                {
                    continue;
                }

                lows.Add(renderer.bounds.min.y);
            }

            BoxCollider2D[] boxes = instance.GetComponentsInChildren<BoxCollider2D>(true);
            IEnumerable<BoxCollider2D> considered = boxes.Any(box => !box.isTrigger) ? boxes.Where(box => !box.isTrigger) : boxes;
            if (lows.Count == 0)
            {
                foreach (BoxCollider2D box in considered)
                {
                    Vector3 centre = box.transform.TransformPoint(box.offset);
                    lows.Add(centre.y - box.size.y * 0.5f * Mathf.Abs(box.transform.lossyScale.y));
                }
            }

            return lows.Count == 0 ? 0f : lows.Min() - pivot;
        }

        private static void AssignIds(Scene scene, GameObject instance)
        {
            string roomId = FindRoom(scene)?.RoomId ?? "ROOM";
            HashSet<string> taken = CollectIds(scene, instance);
            foreach ((System.Type type, string field, string prefix) in IdFields)
            {
                foreach (Component component in instance.GetComponentsInChildren(type, true))
                {
                    var serialized = new SerializedObject(component);
                    SerializedProperty property = serialized.FindProperty(field);
                    if (property == null)
                    {
                        continue;
                    }

                    // A little door joins two rooms; its ID needs both, so it stays marked until the designer sets it.
                    string id = type == typeof(LittleDoor) ? $"{prefix}_{roomId}_{UnsetMark}" : NextId(taken, $"{prefix}_{roomId}_");
                    property.stringValue = id;
                    taken.Add(id);
                    serialized.ApplyModifiedProperties();
                }
            }
        }

        private static string NextId(HashSet<string> taken, string stem)
        {
            for (int number = 1; ; number++)
            {
                string id = stem + number.ToString("00");
                if (!taken.Contains(id))
                {
                    return id;
                }
            }
        }

        private static HashSet<string> CollectIds(Scene scene, GameObject except)
        {
            var ids = new HashSet<string>();
            foreach ((System.Type type, string field, string _) in IdFields)
            {
                foreach (Component component in Components(scene, type))
                {
                    if (except != null && component.transform.IsChildOf(except.transform))
                    {
                        continue;
                    }

                    SerializedProperty property = new SerializedObject(component).FindProperty(field);
                    if (property != null)
                    {
                        ids.Add(property.stringValue);
                    }
                }
            }

            return ids;
        }

        private static void BindClockMarker(Scene scene, GameObject instance)
        {
            if (!instance.TryGetComponent(out ClockMarker marker))
            {
                return;
            }

            ClockGuillotine nearest = Components(scene, typeof(ClockGuillotine)).Cast<ClockGuillotine>()
                .OrderBy(found => Vector2.Distance(found.transform.position, instance.transform.position))
                .FirstOrDefault();
            if (nearest == null)
            {
                return;
            }

            var serialized = new SerializedObject(marker);
            serialized.FindProperty("_mechanism").objectReferenceValue = nearest;
            serialized.ApplyModifiedProperties();
        }

        private static IEnumerable<Component> Components(Scene scene, System.Type type)
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren(type, true));
        }

        /// <summary>Everything that would break travel, saves or the shown art; empty when the room is fine.</summary>
        public static List<string> Validate(Scene scene)
        {
            var problems = new List<string>();
            Room room = FindRoom(scene);
            if (room == null)
            {
                problems.Add("Нет компонента Room: у комнаты нет ID, границ камеры и стартовой точки.");
                return problems;
            }

            if (string.IsNullOrEmpty(room.RoomId) || room.RoomId == DefaultRoomId)
            {
                problems.Add("У Room не задан ID комнаты.");
            }

            var catalog = AssetDatabase.LoadAssetAtPath<RoomCatalog>(CatalogPath);
            HashSet<string> known = catalog != null && catalog.Rooms != null
                ? new HashSet<string>(catalog.Rooms.Select(entry => entry.RoomId))
                : new HashSet<string>();
            if (!known.Contains(room.RoomId))
            {
                problems.Add($"Комнаты {room.RoomId} нет в каталоге комнат (Data/World/Rooms.asset).");
            }

            var seen = new Dictionary<string, int>();
            foreach ((System.Type type, string field, string _) in IdFields)
            {
                foreach (Component component in Components(scene, type))
                {
                    string id = new SerializedObject(component).FindProperty(field)?.stringValue ?? string.Empty;
                    seen[id] = seen.TryGetValue(id, out int count) ? count + 1 : 1;
                    if (id.Contains(UnsetMark))
                    {
                        problems.Add($"{component.name}: ID {id} не дописан (маленькой двери нужны обе комнаты).");
                    }
                }
            }

            foreach (KeyValuePair<string, int> pair in seen.Where(pair => pair.Value > 1))
            {
                problems.Add($"ID {pair.Key} встречается {pair.Value} раза.");
            }

            foreach (RoomDoorway doorway in Components(scene, typeof(RoomDoorway)).Cast<RoomDoorway>())
            {
                if (string.IsNullOrEmpty(doorway.TargetRoomId))
                {
                    problems.Add($"Проём {doorway.DoorwayId}: не задано, куда он ведёт.");
                }
                else if (!known.Contains(doorway.TargetRoomId))
                {
                    problems.Add($"Проём {doorway.DoorwayId} ведёт в {doorway.TargetRoomId}, которой нет в каталоге.");
                }
            }

            foreach (ClockMarker marker in Components(scene, typeof(ClockMarker)).Cast<ClockMarker>())
            {
                if (new SerializedObject(marker).FindProperty("_mechanism").objectReferenceValue == null)
                {
                    problems.Add($"{marker.name}: метка часов не привязана к механизму.");
                }
            }

            if (!Components(scene, typeof(DecreeCache)).Any())
            {
                problems.Add("Нет ромашки потерянных указов (Служебное → DecreeCache): потеря в этой комнате не покажется.");
            }

            return problems;
        }

        /// <summary>Fits the camera bounds to the painted solid cells.</summary>
        public static bool FitCamera(Scene scene)
        {
            Room room = FindRoom(scene);
            if (room == null || !AliceLevelGeometry.TryGetSolidBounds(scene, out Rect bounds))
            {
                return false;
            }

            Undo.RecordObject(room, "Fit camera bounds");
            room.ControlsCamera = true;
            room.CameraBounds = bounds;
            EditorUtility.SetDirty(room);
            EditorSceneManager.MarkSceneDirty(scene);
            return true;
        }

        public static void SetStart(Scene scene, Vector2 position)
        {
            Room room = FindRoom(scene);
            if (room == null)
            {
                return;
            }

            Undo.RecordObject(room, "Set room start");
            room.StartPosition = position;
            EditorUtility.SetDirty(room);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        /// <summary>
        /// A new room scene with its Room, a decree pile and an empty level grid, listed in the room catalog and
        /// the build. Asks to save open changes first; returns false if the user cancels or the ID is taken.
        /// </summary>
        public static bool CreateRoom(string roomId, out string message)
        {
            roomId = (roomId ?? string.Empty).Trim();
            string path = $"{NewRoomsFolder}/{roomId}.unity";
            if (string.IsNullOrEmpty(roomId) || roomId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                message = "Нужен ID комнаты, например L01.";
                return false;
            }

            if (File.Exists(path))
            {
                message = $"Сцена {path} уже есть.";
                return false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                message = "Отменено.";
                return false;
            }

            AliceProjectSetup.EnsureFolder(NewRoomsFolder);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var room = new GameObject("Room").AddComponent<Room>();
            room.RoomId = roomId;
            room.ControlsCamera = true;
            room.CameraBounds = new Rect(0f, 0f, WorldMetricsWidth, WorldMetricsHeight);
            room.StartPosition = new Vector2(4f, 1f);
            AliceLevelGeometry.EnsureRoot(scene, "World");
            Transform gameplay = AliceLevelGeometry.EnsureRoot(scene, GameplayName);
            var cache = AssetDatabase.LoadAssetAtPath<GameObject>(DecreeCachePrefab);
            if (cache != null)
            {
                var pile = (GameObject)PrefabUtility.InstantiatePrefab(cache, scene);
                pile.transform.SetParent(gameplay, false);
            }

            if (!EditorSceneManager.SaveScene(scene, path))
            {
                message = "Не удалось сохранить " + path;
                return false;
            }

            AddToCatalog(roomId);
            AlicePrototypeBuilder.AddToBuildSettings(path);
            message = $"Создана {path}; комната добавлена в каталог и сборку.";
            return true;
        }

        // One screen of the game: a starting size for the camera bounds until geometry is painted.
        private const float WorldMetricsWidth = Alice.Core.WorldMetrics.ViewWidthTiles;
        private const float WorldMetricsHeight = 23f;

        internal static void AddToCatalog(string roomId)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RoomCatalog>(CatalogPath);
            if (catalog == null)
            {
                return;
            }

            List<RoomCatalog.Entry> entries = (catalog.Rooms ?? new RoomCatalog.Entry[0]).ToList();
            if (entries.Any(entry => entry.RoomId == roomId))
            {
                return;
            }

            entries.Add(new RoomCatalog.Entry(roomId, roomId, string.Empty));
            catalog.Rooms = entries.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }
    }
}
