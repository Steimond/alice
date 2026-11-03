using Alice.Combat;
using Alice.Player;
using Alice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace Alice.Editor
{
    /// <summary>
    /// Menu commands that let an agent drive Play mode through Pipeline (<c>editor invoke_menu</c> plus
    /// <c>runtime simulate_key</c> / <c>simulate_pointer</c>) while the Editor sits in the background.
    /// Nothing here is saved: the input routing lives in the in-memory Input System settings and resets
    /// when the Editor restarts, so the owner's normal focus behaviour stays the default.
    /// </summary>
    public static class AlicePlayAutomation
    {
        // Inside Alice's thrust reach (2.75 tiles) and the soldier's attack start range.
        private const float BesideEnemyDistance = 2.4f;
        private const double TapSeconds = 0.15;
        private const float CameraHeight = 2.5f;

        [MenuItem("Alice/Automation/Route Input To Game View")]
        public static void RouteInputToGameView()
        {
            // By default keyboard and mouse reach the game only while the Game view has focus,
            // so simulated presses vanish when another app is in front.
            InputSettings settings = InputSystem.settings;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.runInBackground = true;
            Debug.Log("ALICE_AUTOMATION input routed to the Game view for this Editor session.");
        }

        [MenuItem("Alice/Automation/Press Attack")]
        public static void PressAttack()
        {
            TapButton(Mouse.current?.leftButton);
        }

        [MenuItem("Alice/Automation/Press Jump")]
        public static void PressJump()
        {
            TapButton(Keyboard.current?.spaceKey);
        }

        /// <summary>
        /// Presses a real device button for a moment. Queued events are dropped while the Editor is in the
        /// background, so the state is written straight into the device; actions still see a normal press.
        /// </summary>
        private static void TapButton(ButtonControl button)
        {
            if (!EditorApplication.isPlaying || button == null)
            {
                Debug.LogWarning("ALICE_AUTOMATION presses work in Play mode on an existing device only.");
                return;
            }

            // Changed inside a game input update, so WasPressedThisFrame sees the press in that frame.
            double releaseAt = double.MaxValue;
            void Step()
            {
                if (!EditorApplication.isPlaying)
                {
                    InputSystem.onBeforeUpdate -= Step;
                    return;
                }

                if (InputState.currentUpdateType != InputUpdateType.Dynamic)
                {
                    return;
                }

                if (releaseAt == double.MaxValue)
                {
                    SetButton(button, true);
                    releaseAt = EditorApplication.timeSinceStartup + TapSeconds;
                    return;
                }

                if (EditorApplication.timeSinceStartup >= releaseAt)
                {
                    InputSystem.onBeforeUpdate -= Step;
                    SetButton(button, false);
                }
            }

            InputSystem.onBeforeUpdate += Step;
            Debug.Log($"ALICE_AUTOMATION pressed {button.path}.");
        }

        private static void SetButton(ButtonControl button, bool pressed)
        {
            using (StateEvent.From(button.device, out InputEventPtr eventPtr))
            {
                button.WriteValueIntoEvent(pressed ? 1f : 0f, eventPtr);
                InputState.Change(button.device, eventPtr);
            }
        }

        [MenuItem("Alice/Automation/Log Input State")]
        public static void LogInputState()
        {
            InputSettings settings = InputSystem.settings;
            System.Text.StringBuilder report = new System.Text.StringBuilder("ALICE_AUTOMATION input: ");
            report.Append($"editor={settings.editorInputBehaviorInPlayMode} background={settings.backgroundBehavior} ");
            foreach (InputDevice device in InputSystem.devices)
            {
                report.Append($"[{device.name} enabled={device.enabled} canRunInBackground={device.canRunInBackground}");
                if (device is Keyboard keyboard)
                {
                    report.Append($" space={keyboard.spaceKey.isPressed}");
                }

                if (device is Mouse mouse)
                {
                    report.Append($" left={mouse.leftButton.isPressed}");
                }

                report.Append("] ");
            }

            Debug.Log(report.ToString());
        }

        [MenuItem("Alice/Automation/Put Alice Beside Nearest Enemy")]
        public static void PutAliceBesideNearestEnemy()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("ALICE_AUTOMATION works in Play mode only, so the scene is never changed.");
                return;
            }

            PlayerMotor alice = Object.FindAnyObjectByType<PlayerMotor>();
            if (alice == null)
            {
                Debug.LogWarning("ALICE_AUTOMATION found no Alice in the scene.");
                return;
            }

            MeleeEnemy nearest = null;
            float best = float.MaxValue;
            foreach (MeleeEnemy enemy in Object.FindObjectsByType<MeleeEnemy>())
            {
                float distance = Mathf.Abs(enemy.transform.position.x - alice.Position.x);
                if (enemy.CurrentState != MeleeEnemy.State.Dead && distance < best)
                {
                    best = distance;
                    nearest = enemy;
                }
            }

            if (nearest == null)
            {
                Debug.LogWarning("ALICE_AUTOMATION found no living enemy.");
                return;
            }

            // On the side the enemy faces, so it notices her at once and she faces it after one step.
            Vector2 target = (Vector2)nearest.transform.position + new Vector2(nearest.Facing * BesideEnemyDistance, 0.02f);
            alice.Teleport(target);

            // The follow camera eases over seconds, longer still under a slowed time scale; jump it there.
            Camera camera = Camera.main;
            if (camera != null && camera.TryGetComponent(out CameraFollow2D _))
            {
                camera.transform.position = new Vector3(target.x + 0.5f * nearest.Facing * -BesideEnemyDistance,
                    target.y + CameraHeight, camera.transform.position.z);
            }

            Debug.Log($"ALICE_AUTOMATION Alice placed at {target.x:0.##} beside {nearest.name}.");
        }
    }
}
