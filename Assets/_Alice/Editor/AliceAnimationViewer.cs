using System.Collections.Generic;
using System.Linq;
using Alice.Combat;
using UnityEditor;
using UnityEngine;

namespace Alice.Editor
{
    /// <summary>
    /// Every animation of the game in one window, played in real time or stepped frame by frame, enlarged with
    /// whole-pixel zoom on the room grey with the floor line (owner's request, 9 November 2026). It reads the frame
    /// lists the game itself plays: enemy clip sets with their own frame times, and every sprite list of the other
    /// data assets (Alice's frames, the card soldier), where a strike's wind-up, blow and recovery play as one clip.
    /// </summary>
    public sealed class AliceAnimationViewer : EditorWindow
    {
        private const string DataFolder = "Assets/_Alice/Data";
        private const float ListWidth = 230f;

        private static readonly Color Room = new Color(0.23f, 0.23f, 0.25f);
        private static readonly Color Floor = new Color(0.55f, 0.35f, 0.35f);

        private sealed class Entry
        {
            public string Name;
            public Sprite[] Frames;
            public float FrameTime;
            public bool Loop;
        }

        private sealed class Source
        {
            public string Name;
            public List<Entry> Clips = new List<Entry>();
            public bool Open;
        }

        private List<Source> _sources = new List<Source>();
        private Entry _clip;
        private int _frame;
        private bool _playing = true;
        private float _speed = 1f;
        private int _zoom = 4;
        private bool _onion;
        private double _clock;
        private double _lastTick;
        private Vector2 _listScroll;
        private string _filter = string.Empty;

        [MenuItem("Alice/Animation Viewer", false, 3)]
        private static void Open()
        {
            GetWindow<AliceAnimationViewer>("Animations").Show();
        }

        private void OnEnable()
        {
            Reload();
            _lastTick = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
        }

        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            double delta = now - _lastTick;
            _lastTick = now;
            if (!_playing || _clip == null || _clip.Frames.Length == 0)
            {
                return;
            }

            _clock += delta * _speed;
            float step = Mathf.Max(0.01f, _clip.FrameTime);
            int frame = Mathf.FloorToInt((float)(_clock / step));
            if (_clip.Loop)
            {
                frame %= _clip.Frames.Length;
            }
            else if (frame >= _clip.Frames.Length)
            {
                // A one-shot holds its last frame for a moment, then starts over, so it can be watched again.
                frame = _clip.Frames.Length - 1;
                if (_clock > step * (_clip.Frames.Length + 4))
                {
                    _clock = 0;
                    frame = 0;
                }
            }

            if (frame != _frame)
            {
                _frame = frame;
                Repaint();
            }
        }

        private void Reload()
        {
            _sources = new List<Source>();
            Dictionary<string, string> executionOwners = ExecutionOwners();
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { DataFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null)
                {
                    continue;
                }

                var source = new Source { Name = SubjectOf(asset.name) };
                if (asset is EnemyClipSet clips)
                {
                    foreach (EnemyClipSet.Clip clip in clips.Clips.OrderBy(clip => clip.Id))
                    {
                        if (clip.Frames != null && clip.Frames.Length > 0)
                        {
                            source.Clips.Add(new Entry
                            {
                                Name = clip.Id, Frames = clip.Frames, FrameTime = clip.FrameTime, Loop = clip.Loop
                            });
                        }
                    }
                }
                else
                {
                    CollectSpriteLists(asset, source.Clips);
                }

                foreach (Entry clip in source.Clips)
                {
                    clip.Name = source.Name + "_" + ActionOf(source.Name, clip.Name, executionOwners);
                }

                source.Clips.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
                if (source.Clips.Count > 0)
                {
                    _sources.Add(source);
                }
            }

            _sources = _sources.OrderBy(source => source.Name).ToList();
        }

        // Names read "<who>_<what>" everywhere (owner, 10 November 2026: the names were all different and did not say
        // what belongs to what). An execution is "<enemy>_execution" in the enemy's list and
        // "alice_execution_<enemy>" in Alice's, so the two halves of one execution find each other by name.
        private static readonly Dictionary<string, string> AliceActions = new Dictionary<string, string>
        {
            { "idleBreath", "idle" }, { "idleFidget", "idle_fidget" }, { "run", "run" },
            { "jumpRise", "jump_rise" }, { "jumpApex", "jump_apex" }, { "fallLoop", "jump_fall" },
            { "landing", "jump_land" }, { "stepOff", "jump_step_off" }, { "slideLoop", "slide" },
            { "slash", "attack_1_slash" }, { "cut", "attack_2_cut" }, { "finisher", "attack_3_finisher" },
            { "airSlash", "attack_air" }, { "parry", "parry" }, { "counter", "parry_counter" },
            { "spin_strike", "technique_spin" }, { "hurt", "hurt" }, { "defeat", "defeat" }, { "wake", "defeat_wake" },
            { "salts", "heal_salts" }, { "restSit", "rest_sit" }, { "restDoze", "rest_doze" }, { "pickup", "pickup" },
            { "tearFree", "sheet_tear_free" }, { "bite", "item_bite" }, { "shrink", "item_shrink" },
        };

        private static readonly Dictionary<string, string> EnemyActions = new Dictionary<string, string>
        {
            { "death_execution", "execution" }, { "deathair", "death_air" }, { "deathfloor", "death_floor" },
            { "opened", "stunned_by_parry" },
        };

        private static readonly Dictionary<string, Dictionary<string, string>> SubjectActions =
            new Dictionary<string, Dictionary<string, string>>
            {
                // The sheet's cut and torn clips are its deaths by those blows.
                { "sheet", new Dictionary<string, string> { { "cut", "death_cut" }, { "torn", "death_torn" } } },
            };

        /// <summary>"AliceSprites" -> "alice", "CardSoldierSprites" -> "card_soldier", "DuckClips" -> "duck".</summary>
        private static string SubjectOf(string assetName)
        {
            string bare = assetName;
            foreach (string suffix in new[] { "Sprites", "Clips" })
            {
                if (bare.Length > suffix.Length && bare.EndsWith(suffix, System.StringComparison.Ordinal))
                {
                    bare = bare.Substring(0, bare.Length - suffix.Length);
                }
            }

            return Snake(bare);
        }

        private static string ActionOf(string subject, string raw, Dictionary<string, string> executionOwners)
        {
            if (subject == "alice")
            {
                if (AliceActions.TryGetValue(raw, out string action))
                {
                    return action;
                }

                if (raw.StartsWith("small", System.StringComparison.Ordinal) && raw.Length > 5)
                {
                    return "small_" + Snake(raw.Substring(5)).Replace("landing", "land");
                }

                // Alice's half of an execution: named by the enemy whose settings carry this execution ID.
                if (executionOwners.TryGetValue(raw, out string owner))
                {
                    return "execution_" + owner;
                }

                int cut = raw.IndexOf('_');
                return cut > 0 ? "execution_" + raw.Substring(0, cut) : Snake(raw);
            }

            if (SubjectActions.TryGetValue(subject, out Dictionary<string, string> own) && own.TryGetValue(raw, out string mine))
            {
                return mine;
            }

            return EnemyActions.TryGetValue(raw, out string mapped) ? mapped : Snake(raw);
        }

        /// <summary>Execution ID -> enemy subject, from every enemy settings asset that names one.</summary>
        private static Dictionary<string, string> ExecutionOwners()
        {
            var owners = new Dictionary<string, string>();
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { DataFolder }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null)
                {
                    continue;
                }

                SerializedProperty id = new SerializedObject(asset).FindProperty("_executionId");
                if (id != null && id.propertyType == SerializedPropertyType.String && !string.IsNullOrEmpty(id.stringValue))
                {
                    owners[id.stringValue] = SubjectOf(asset.name);
                }
            }

            return owners;
        }

        private static string Snake(string camel)
        {
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < camel.Length; i++)
            {
                char c = camel[i];
                if (char.IsUpper(c) && i > 0 && camel[i - 1] != '_')
                {
                    text.Append('_');
                }

                text.Append(char.ToLowerInvariant(c));
            }

            return text.ToString();
        }

        /// <summary>
        /// Every sprite array of the asset as one whole animation under its own short name: a strike's wind-up, blow
        /// and recovery (stored as three lists) play as one clip, never as pieces of two or three frames.
        /// </summary>
        private static void CollectSpriteLists(ScriptableObject asset, List<Entry> into)
        {
            var order = new List<string>();
            var wholes = new Dictionary<string, List<Sprite>>();
            var serialized = new SerializedObject(asset);
            SerializedProperty property = serialized.GetIterator();
            while (property.Next(true))
            {
                if (!property.isArray || property.propertyType != SerializedPropertyType.Generic
                    || property.arrayElementType != "PPtr<$Sprite>")
                {
                    continue;
                }

                // The iterator also enters each array as its inner "Array" node; that would be every list again.
                if (property.name == "Array")
                {
                    continue;
                }

                Sprite[] frames = Enumerable.Range(0, property.arraySize)
                    .Select(i => property.GetArrayElementAtIndex(i).objectReferenceValue as Sprite)
                    .Where(sprite => sprite != null).ToArray();
                if (frames.Length == 0)
                {
                    continue;
                }

                string name = NameOf(property);
                if (!wholes.TryGetValue(name, out List<Sprite> whole))
                {
                    whole = new List<Sprite>();
                    wholes[name] = whole;
                    order.Add(name);
                }

                whole.AddRange(frames);
            }

            foreach (string name in order)
            {
                into.Add(new Entry { Name = name, Frames = wholes[name].ToArray(), FrameTime = 0.08f, Loop = true });
            }

            into.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        private static readonly string[] PhaseSuffixes = { "Windup", "Active", "Recovery" };

        // "_fooWindup" and "_fooActive" are both "foo"; "_executions.Array.data[2]._frames._windup" is the
        // execution's own ID ("moth_pin"); any other list is its field name ("run", "jumpRise").
        private static string NameOf(SerializedProperty property)
        {
            string leaf = property.name.TrimStart('_');
            // A death variant ("_deathVariants.Array.data[1]._frames") is named by its blow: "death_slash".
            int element = property.propertyPath.LastIndexOf(']');
            if (leaf == "frames" && element > 0)
            {
                SerializedProperty kind = property.serializedObject.FindProperty(
                    property.propertyPath.Substring(0, element + 1) + "._kind");
                if (kind != null && kind.propertyType == SerializedPropertyType.Enum)
                {
                    return "death_" + kind.enumNames[kind.enumValueIndex].ToLowerInvariant();
                }
            }

            if (leaf == "windup" || leaf == "active" || leaf == "recovery")
            {
                return LastIdOnPath(property) ?? ParentField(property);
            }

            foreach (string suffix in PhaseSuffixes)
            {
                if (leaf.Length > suffix.Length && leaf.EndsWith(suffix))
                {
                    return leaf.Substring(0, leaf.Length - suffix.Length);
                }
            }

            return leaf;
        }

        private static string LastIdOnPath(SerializedProperty property)
        {
            string path = property.propertyPath;
            string found = null;
            int at = path.IndexOf(".Array.data[", System.StringComparison.Ordinal);
            while (at >= 0)
            {
                int close = path.IndexOf(']', at);
                SerializedProperty id = property.serializedObject.FindProperty(path.Substring(0, close + 1) + "._id");
                if (id != null && !string.IsNullOrEmpty(id.stringValue))
                {
                    found = id.stringValue;
                }

                at = path.IndexOf(".Array.data[", close, System.StringComparison.Ordinal);
            }

            return found;
        }

        private static string ParentField(SerializedProperty property)
        {
            string[] steps = property.propertyPath.Split('.');
            return steps.Length >= 2 ? steps[steps.Length - 2].TrimStart('_') : property.name.TrimStart('_');
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawList();
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawControls();
                    DrawPreview(GUILayoutUtility.GetRect(10f, 10000f, 10f, 10000f));
                }
            }

            HandleKeys();
        }

        private void DrawList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(ListWidth)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarSearchField);
                    if (GUILayout.Button("Reload", EditorStyles.miniButton, GUILayout.Width(52f)))
                    {
                        Reload();
                    }
                }

                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                foreach (Source source in _sources)
                {
                    List<Entry> shown = source.Clips.Where(Matches(source)).ToList();
                    if (shown.Count == 0)
                    {
                        continue;
                    }

                    source.Open = EditorGUILayout.Foldout(source.Open || _filter.Length > 0, source.Name, true);
                    if (!source.Open && _filter.Length == 0)
                    {
                        continue;
                    }

                    foreach (Entry clip in shown)
                    {
                        var style = clip == _clip ? EditorStyles.whiteLabel : EditorStyles.label;
                        string label = $"   {clip.Name}  ({clip.Frames.Length})";
                        if (GUILayout.Button(label, style))
                        {
                            Select(clip);
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private System.Func<Entry, bool> Matches(Source source)
        {
            string filter = _filter.Trim().ToLowerInvariant();
            return clip => filter.Length == 0 || source.Name.ToLowerInvariant().Contains(filter)
                || clip.Name.ToLowerInvariant().Contains(filter);
        }

        private void Select(Entry clip)
        {
            _clip = clip;
            _frame = 0;
            _clock = 0;
            Repaint();
        }

        private void DrawControls()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(_playing ? "Pause" : "Play", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                {
                    _playing = !_playing;
                    _clock = _frame * (_clip?.FrameTime ?? 0.08f);
                }

                if (GUILayout.Button("◀", EditorStyles.toolbarButton, GUILayout.Width(26f)))
                {
                    Step(-1);
                }

                if (GUILayout.Button("▶", EditorStyles.toolbarButton, GUILayout.Width(26f)))
                {
                    Step(1);
                }

                GUILayout.Label("Speed", GUILayout.Width(40f));
                _speed = GUILayout.HorizontalSlider(_speed, 0.1f, 2f, GUILayout.Width(80f));
                GUILayout.Label(_speed.ToString("0.0") + "x", GUILayout.Width(32f));
                GUILayout.Label("Zoom", GUILayout.Width(36f));
                _zoom = Mathf.RoundToInt(GUILayout.HorizontalSlider(_zoom, 1f, 10f, GUILayout.Width(80f)));
                GUILayout.Label(_zoom + "x", GUILayout.Width(26f));
                _onion = GUILayout.Toggle(_onion, "Onion skin", EditorStyles.toolbarButton, GUILayout.Width(76f));
                GUILayout.FlexibleSpace();
            }

            if (_clip == null)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                int frame = EditorGUILayout.IntSlider(_frame, 0, _clip.Frames.Length - 1);
                if (frame != _frame)
                {
                    _playing = false;
                    _frame = frame;
                }

                _clip.FrameTime = Mathf.Max(0.01f,
                    EditorGUILayout.FloatField("Frame time", _clip.FrameTime, GUILayout.Width(200f)));
            }

            Sprite sprite = _clip.Frames[_frame];
            EditorGUILayout.LabelField($"{sprite.name}   {sprite.rect.width}×{sprite.rect.height}   "
                + "Space: play/pause   ← →: frame", EditorStyles.miniLabel);
        }

        private void Step(int by)
        {
            if (_clip == null)
            {
                return;
            }

            _playing = false;
            int count = _clip.Frames.Length;
            _frame = (_frame + by + count) % count;
            Repaint();
        }

        private void HandleKeys()
        {
            Event current = Event.current;
            if (current.type != EventType.KeyDown || GUIUtility.keyboardControl != 0)
            {
                return;
            }

            switch (current.keyCode)
            {
                case KeyCode.Space:
                    _playing = !_playing;
                    current.Use();
                    break;
                case KeyCode.LeftArrow:
                    Step(-1);
                    current.Use();
                    break;
                case KeyCode.RightArrow:
                    Step(1);
                    current.Use();
                    break;
            }
        }

        private void DrawPreview(Rect area)
        {
            EditorGUI.DrawRect(area, Room);
            if (_clip == null || _clip.Frames.Length == 0)
            {
                GUI.Label(area, "Pick a clip on the left.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            // One pivot for every frame, as in the game: the sprite pivot sits on the floor line in the middle.
            Vector2 origin = new Vector2(Mathf.Round(area.center.x), Mathf.Round(area.yMax - 40f));
            EditorGUI.DrawRect(new Rect(area.x, origin.y, area.width, 1f), Floor);
            if (_onion)
            {
                int previous = (_frame - 1 + _clip.Frames.Length) % _clip.Frames.Length;
                DrawSprite(_clip.Frames[previous], origin, new Color(1f, 1f, 1f, 0.3f));
            }

            DrawSprite(_clip.Frames[_frame], origin, Color.white);
        }

        private void DrawSprite(Sprite sprite, Vector2 pivotAt, Color tint)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            Rect rect = sprite.textureRect;
            Texture2D texture = sprite.texture;
            var size = new Vector2(rect.width, rect.height) * _zoom;
            Vector2 pivot = sprite.pivot * _zoom;
            var target = new Rect(pivotAt.x - pivot.x, pivotAt.y - (size.y - pivot.y), size.x, size.y);
            var uv = new Rect(rect.x / texture.width, rect.y / texture.height, rect.width / texture.width,
                rect.height / texture.height);
            Color previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(target, texture, uv);
            GUI.color = previous;
        }
    }
}
