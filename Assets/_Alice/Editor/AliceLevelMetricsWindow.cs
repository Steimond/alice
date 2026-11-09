using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Alice.Editor
{
    /// <summary>
    /// Alice → Level → Sizes, also the «Размеры» button of the level palette: every size a room is built against,
    /// grouped, recomputed each time the window opens (<see cref="AliceLevelMetrics"/>). The list can be copied as a
    /// Markdown table. Labels are Russian: the window is for the owner.
    /// </summary>
    public sealed class AliceLevelMetricsWindow : EditorWindow
    {
        private const float NameWidth = 250f;
        private const float ValueWidth = 190f;

        private List<AliceLevelMetrics.Group> _groups;
        private Vector2 _scroll;
        private GUIStyle _wrap;

        [MenuItem("Alice/Level/Sizes")]
        public static void Open()
        {
            var window = GetWindow<AliceLevelMetricsWindow>(true, "Размеры для левел-дизайна");
            window.minSize = new Vector2(640f, 420f);
            window.Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Refresh()
        {
            _groups = AliceLevelMetrics.Build();
        }

        private void OnGUI()
        {
            _wrap ??= new GUIStyle(EditorStyles.label) { wordWrap = true };
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Всё в тайлах (1 тайл = 1 юнит = 16 пикселей), считается из констант и данных игры.", EditorStyles.miniLabel);
                if (GUILayout.Button(new GUIContent("Обновить", "Пересчитать после правки данных"), GUILayout.Width(80f)))
                {
                    Refresh();
                }

                if (GUILayout.Button(new GUIContent("Копировать", "Скопировать списком в Markdown"), GUILayout.Width(90f)))
                {
                    EditorGUIUtility.systemCopyBuffer = AliceLevelMetrics.ToMarkdown(_groups);
                    ShowNotification(new GUIContent("Скопировано"));
                }
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (AliceLevelMetrics.Group group in _groups)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField(group.Title, EditorStyles.boldLabel);
                foreach (AliceLevelMetrics.Row row in group.Rows)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(row.Name, GUILayout.Width(NameWidth));
                        EditorGUILayout.SelectableLabel(row.Value, EditorStyles.boldLabel, GUILayout.Width(ValueWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                        EditorGUILayout.LabelField(row.Note, _wrap);
                    }
                }
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox("Дальности — без ширины тела и запаса после края, поэтому на деле Алиса долетает чуть дальше. Строить по пределам из первой группы, остальное — для расчёта расстояний.", MessageType.None);
            EditorGUILayout.EndScrollView();
        }
    }
}
