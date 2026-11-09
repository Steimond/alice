using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Alice.Combat;
using Alice.Core;
using Alice.Player;
using UnityEditor;
using UnityEngine;

namespace Alice.Editor
{
    /// <summary>
    /// Every size a room is built against, worked out from the game's own numbers: the traversal constants
    /// (<see cref="TraversalMetrics"/>), the movement and enemy data and the gate prefabs. Nothing is typed in by
    /// hand, so the list stays true when a value changes. Distances follow the motor's physics: rising under
    /// gravity, falling under the heavier fall gravity up to the fall-speed cap; the body's width and coyote time
    /// are not added, so the real reach is a little longer than listed. Labels are Russian: the list is for the owner.
    /// </summary>
    public static class AliceLevelMetrics
    {
        public readonly struct Row
        {
            public readonly string Name;
            public readonly string Value;
            public readonly string Note;

            public Row(string name, string value, string note = "")
            {
                Name = name;
                Value = value;
                Note = note;
            }
        }

        public sealed class Group
        {
            public string Title;
            public readonly List<Row> Rows = new List<Row>();

            public Group Add(string name, string value, string note = "")
            {
                Rows.Add(new Row(name, value, note));
                return this;
            }
        }

        private const string MovementPath = "Assets/_Alice/Data/Player/AliceMovement.asset";
        private const string SoldierPath = "Assets/_Alice/Data/Enemies/CardSoldier.asset";
        private const string AlicePrefabPath = "Assets/_Alice/Prefabs/Characters/Alice.prefab";
        private const string BridgePrefabPath = "Assets/_Alice/Prefabs/Level/Gates/CheshireBridge.prefab";
        private const string GuillotinePrefabPath = "Assets/_Alice/Prefabs/Level/Gates/Guillotine.prefab";

        private static float RiseGravity => TraversalMetrics.Gravity;
        private static float FallGravity => TraversalMetrics.Gravity * TraversalMetrics.FallGravityMultiplier;

        /// <summary>Time to fall <paramref name="height"/> tiles from rest, with the fall-speed cap.</summary>
        public static float FallTime(float height)
        {
            if (height <= 0f)
            {
                return 0f;
            }

            float capTime = TraversalMetrics.MaxFallSpeed / FallGravity;
            float capHeight = TraversalMetrics.MaxFallSpeed * capTime * 0.5f;
            return height <= capHeight
                ? Mathf.Sqrt(2f * height / FallGravity)
                : capTime + (height - capHeight) / TraversalMetrics.MaxFallSpeed;
        }

        /// <summary>Time to rise <paramref name="height"/> tiles to a standstill.</summary>
        public static float RiseTime(float height)
        {
            return Mathf.Sqrt(2f * Mathf.Max(0f, height) / RiseGravity);
        }

        /// <summary>
        /// How far a full running jump carries Alice before she lands <paramref name="landHeight"/> tiles above
        /// (or below, when negative) where she jumped; with the fan used at the top of the jump if asked.
        /// Returns a negative value when she cannot get that high.
        /// </summary>
        public static float RunningJumpReach(float landHeight, bool fan = false)
        {
            float peak = TraversalMetrics.JumpHeight + (fan ? TraversalMetrics.FanJumpHeight : 0f);
            if (landHeight > peak)
            {
                return -1f;
            }

            float air = TraversalMetrics.TimeToApex + (fan ? RiseTime(TraversalMetrics.FanJumpHeight) : 0f) + FallTime(peak - landHeight);
            return TraversalMetrics.MaxRunSpeed * air;
        }

        public static List<Group> Build()
        {
            var movement = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>(MovementPath);
            var soldier = AssetDatabase.LoadAssetAtPath<MeleeEnemySettings>(SoldierPath);
            var groups = new List<Group>();
            float jump = TraversalMetrics.JumpHeight;
            float fan = TraversalMetrics.FanJumpHeight;
            float run = TraversalMetrics.MaxRunSpeed;
            float body = TraversalMetrics.StandingSize.y;

            groups.Add(new Group { Title = "Пределы для комнат (строить по ним)" }
                .Add("Уступ прыжком с места", $"до {TraversalMetrics.MaxJumpLedge} тайлов", $"{TraversalMetrics.MaxJumpLedge + 1} — уже недоступно без веера")
                .Add("Провал с разбега", $"до {TraversalMetrics.MaxJumpGap} тайлов", "по ровной опоре с обеих сторон")
                .Add("Уступ с веером (A01)", $"до {TraversalMetrics.MaxFanLedge} тайлов")
                .Add("Провал с веером (A01)", $"до {TraversalMetrics.MaxFanGap} тайлов")
                .Add("Проход стоя", $"от {TraversalMetrics.MinWalkPassageHeight} тайлов в высоту", "лучше 4")
                .Add("Проход только подкатом", $"{T(TraversalMetrics.SlidePassageHeight)} в высоту", "стоя не пройти")
                .Add("Потолок над местом прыжка", $"от {Mathf.CeilToInt(jump + body)} тайлов над опорой", "ниже — Алиса бьётся головой")
                .Add("Потолок, где прыгают с веером", $"от {Mathf.CeilToInt(jump + fan + body)} тайлов над опорой"));

            var jumpGroup = new Group { Title = "Прыжок" }
                .Add("Высота полного прыжка", T(jump), "кнопка удержана до верхней точки")
                .Add("Время до верхней точки", $"{F(TraversalMetrics.TimeToApex, 2)} с");
            if (movement != null)
            {
                float cut = movement.JumpCutMultiplier;
                jumpGroup.Add("Самый короткий прыжок", "≈ " + T(jump * cut * cut), "кнопку отпустили сразу");
            }

            jumpGroup
                .Add("В воздухе, прыжок на ту же высоту", $"{F(TraversalMetrics.TimeToApex + FallTime(jump), 2)} с")
                .Add("Дальность с разбега на ту же высоту", T(RunningJumpReach(0f)), "от точки толчка до приземления");
            for (int height = 1; height <= TraversalMetrics.MaxJumpLedge; height++)
            {
                jumpGroup.Add($"Дальность с разбега на уступ +{height}", T(RunningJumpReach(height)));
            }

            foreach (int drop in new[] { 2, 4, 8 })
            {
                jumpGroup.Add($"Дальность с разбега вниз на {drop}", T(RunningJumpReach(-drop)));
            }

            groups.Add(jumpGroup);

            var fanGroup = new Group { Title = "Веер A01 — второй прыжок в воздухе" }
                .Add("Подъём от точки, где нажат", T(fan), "один раз за полёт, обновляется на опоре")
                .Add("Наибольшая высота", T(jump + fan), "веер в верхней точке прыжка")
                .Add("Дальность с разбега на ту же высоту", T(RunningJumpReach(0f, true)));
            for (int height = TraversalMetrics.MaxJumpLedge + 1; height <= TraversalMetrics.MaxFanLedge; height++)
            {
                fanGroup.Add($"Дальность с разбега на уступ +{height}", T(RunningJumpReach(height, true)));
            }

            groups.Add(fanGroup);

            var runGroup = new Group { Title = "Бег, подкат, падение" }
                .Add("Скорость бега", $"{F(run)} тайла/с")
                .Add("Подкат", $"{T(TraversalMetrics.SlideSpeed * TraversalMetrics.SlideDuration)} за {F(TraversalMetrics.SlideDuration, 2)} с", $"тело {T(TraversalMetrics.SlidingSize.y)} в высоту")
                .Add("Предельная скорость падения", $"{F(TraversalMetrics.MaxFallSpeed)} тайла/с", $"набирается за {F(FallTime(TraversalMetrics.MaxFallSpeed * TraversalMetrics.MaxFallSpeed / (2f * FallGravity)), 2)} с");
            if (movement != null)
            {
                runGroup
                    .Add("Прыжок после края (койот)", $"{F(movement.CoyoteTime, 2)} с", $"≈ {T(movement.CoyoteTime * run)} за краем на бегу")
                    .Add("Запас нажатия прыжка до приземления", $"{F(movement.JumpBufferTime, 2)} с")
                    .Add("Пауза между подкатами", $"{F(movement.SlideCooldown, 2)} с");
            }

            groups.Add(runGroup);

            groups.Add(new Group { Title = "Тело и экран" }
                .Add("Алиса", $"{F(TraversalMetrics.StandingSize.x, 2)} × {F(body, 2)} тайла", "ширина × рост, опора у ног")
                .Add("Экран", $"{F(WorldMetrics.ViewWidthTiles)} × {F(WorldMetrics.ViewHeightTiles)} тайла", "цель прыжка и замах врага должны помещаться")
                .Add("Тайл", $"{WorldMetrics.TilePixels} пикселей"));

            var gates = new Group { Title = "Способности и затворы" };
            float grow = ReadFloat(AlicePrefabPath, "Alice.Player.PlayerSize", "_growDistance");
            if (grow > 0f)
            {
                gates.Add("Гриб A02: маленькая Алиса", $"вырастает через {T(grow)}", "от места, где уменьшилась: проход за маленькой дверью короче");
            }

            float bridge = ReadFloat(BridgePrefabPath, "Alice.World.CheshireBridge", "_reach");
            if (bridge > 0f)
            {
                gates.Add("Чеширский мост A03", $"проявляется с {TFrom(bridge)}", "по умолчанию у префаба");
            }

            float lift = ReadFloat(GuillotinePrefabPath, "Alice.World.ClockGuillotine", "_lift");
            if (lift > 0f)
            {
                gates.Add("Гильотина (часы A04)", $"нож поднимается на {T(lift)}", "по умолчанию у префаба");
            }

            if (gates.Rows.Count > 0)
            {
                groups.Add(gates);
            }

            if (soldier != null)
            {
                groups.Add(new Group { Title = "Карточный солдат" }
                    .Add("Обзор", $"{T(soldier.SightRange)} вперёд, {F(soldier.SightHeight)} по высоте", "не должен видеть точку прибытия у проёма")
                    .Add("Поводок", $"{T(soldier.LeashDistance)} от поста")
                    .Add("Начинает атаку", $"с {TFrom(soldier.AttackStartRange)}")
                    .Add("Выпад", $"{T(soldier.LungeDistance)}, удар до {F(soldier.Reach)}"));
            }

            return groups;
        }

        /// <summary>The list as a Markdown table, for pasting into notes or the level-design guide.</summary>
        public static string ToMarkdown(List<Group> groups)
        {
            var text = new StringBuilder();
            foreach (Group group in groups)
            {
                text.AppendLine($"### {group.Title}").AppendLine();
                text.AppendLine("| Что | Размер | Пояснение |").AppendLine("| --- | --- | --- |");
                foreach (Row row in group.Rows)
                {
                    text.AppendLine($"| {row.Name} | {row.Value} | {row.Note} |");
                }

                text.AppendLine();
            }

            return text.ToString();
        }

        // "4,2 тайла", "1 тайл", "3 тайла", "9 тайлов": a fraction always takes "тайла".
        private static string T(float value)
        {
            string word = IsWhole(value) ? Word(Mathf.RoundToInt(value), "тайл", "тайла", "тайлов") : "тайла";
            return $"{F(value)} {word}";
        }

        // After "с", "до", "от": "с 3 тайлов", "с 1 тайла", "с 3,2 тайла".
        private static string TFrom(float value)
        {
            string word = IsWhole(value) ? Word(Mathf.RoundToInt(value), "тайла", "тайлов", "тайлов") : "тайла";
            return $"{F(value)} {word}";
        }

        // Whole as it is shown, to one decimal.
        private static bool IsWhole(float value)
        {
            return Mathf.Abs(Mathf.Round(value * 10f) % 10f) < 0.5f;
        }

        private static string Word(int number, string one, string few, string many)
        {
            int tens = number % 100;
            int last = number % 10;
            if (tens >= 11 && tens <= 14)
            {
                return many;
            }

            return last == 1 ? one : last >= 2 && last <= 4 ? few : many;
        }

        private static string F(float value, int digits = 1)
        {
            return value.ToString("0." + new string('#', digits), CultureInfo.GetCultureInfo("ru-RU"));
        }

        private static float ReadFloat(string prefabPath, string componentType, string field)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return -1f;
            }

            foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
            {
                if (component != null && component.GetType().FullName == componentType)
                {
                    SerializedProperty property = new SerializedObject(component).FindProperty(field);
                    return property != null ? property.floatValue : -1f;
                }
            }

            return -1f;
        }
    }
}
