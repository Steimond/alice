using Alice.Combat;
using Alice.Core;
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

        private static int _captureIndex;

        [MenuItem("Alice/Automation/Route Input To Game View")]
        public static void RouteInputToGameView()
        {
            RouteInput();
            Debug.Log("ALICE_AUTOMATION input routed to the Game view for this Editor session.");
        }

        /// <summary>
        /// By default keyboard and mouse reach the game only while the Game view has focus, devices are
        /// reset while the Editor is in the background and Play only advances on Editor ticks there, so
        /// presses vanish and frames go stale when another app is in front.
        /// The settings live in memory and fall back to the defaults after a script reload.
        /// </summary>
        private static void RouteInput()
        {
            InputSettings settings = InputSystem.settings;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.runInBackground = true;
        }

        [MenuItem("Alice/Automation/Press Attack")]
        public static void PressAttack()
        {
            TapButton(Mouse.current?.leftButton);
        }

        [MenuItem("Alice/Automation/Press Parry")]
        public static void PressParry()
        {
            TapButton(Keyboard.current?.fKey);
        }

        [MenuItem("Alice/Automation/Press Interact")]
        public static void PressInteract()
        {
            TapButton(Keyboard.current?.eKey);
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

            RouteInput();

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

        /// <summary>
        /// Renders the main camera into a 640x360 image under Temp/AliceCaptures. Pipeline's capture copies
        /// the Game view from the screen, which goes stale while that view is hidden or the Editor is in
        /// the background; rendering the camera directly always shows the current frame.
        /// </summary>
        [MenuItem("Alice/Automation/Capture Camera")]
        public static void CaptureCamera()
        {
            RouteInput();
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("ALICE_AUTOMATION found no main camera.");
                return;
            }

            string folder = System.IO.Path.GetFullPath("Temp/AliceCaptures");
            System.IO.Directory.CreateDirectory(folder);
            string path = System.IO.Path.Combine(folder, $"capture-{++_captureIndex:D3}.png");

            RenderTexture target = RenderTexture.GetTemporary(WorldMetrics.ReferenceWidth, WorldMetrics.ReferenceHeight, 24);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Texture2D image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
                image.Apply();
                System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(image);
            }

            Debug.Log($"ALICE_AUTOMATION captured {path}");
        }

        /// <summary>
        /// The camera with the overlay HUD, at the Game view's size. Overlay canvases are moved onto the camera
        /// for this one render: Unity's own screenshot reads the Game view, which goes stale in the background.
        /// </summary>
        [MenuItem("Alice/Automation/Capture Screen")]
        public static void CaptureScreen()
        {
            RouteInput();
            Camera camera = Camera.main;
            if (!EditorApplication.isPlaying || camera == null)
            {
                Debug.LogWarning("ALICE_AUTOMATION needs a main camera in Play mode.");
                return;
            }

            string folder = System.IO.Path.GetFullPath("Temp/AliceCaptures");
            System.IO.Directory.CreateDirectory(folder);
            string path = System.IO.Path.Combine(folder, $"screen-{++_captureIndex:D3}.png");

            Canvas[] overlays = System.Array.FindAll(Object.FindObjectsByType<Canvas>(),
                canvas => canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay);
            RenderTexture target = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Texture2D image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                foreach (Canvas canvas in overlays)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = camera.nearClipPlane + 0.01f;
                }

                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
                image.Apply();
                System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                foreach (Canvas canvas in overlays)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }

                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(image);
            }

            Debug.Log($"ALICE_AUTOMATION captured {path}");
        }

        [MenuItem("Alice/Automation/Press Slide")]
        public static void PressSlide()
        {
            TapButton(Keyboard.current?.leftShiftKey);
        }

        [MenuItem("Alice/Automation/Press Ability")]
        public static void PressAbility()
        {
            TapButton(Mouse.current?.rightButton);
        }

        /// <summary>Fills Alice's confidence, so a technique can be tried without beating enemies first.</summary>
        [MenuItem("Alice/Automation/Fill Confidence")]
        public static void FillConfidence()
        {
            RouteInput();
            PlayerSpoils spoils = EditorApplication.isPlaying ? Object.FindAnyObjectByType<PlayerSpoils>() : null;
            if (spoils == null || spoils.Confidence == null)
            {
                Debug.LogWarning("ALICE_AUTOMATION needs Alice in Play mode.");
                return;
            }

            spoils.Confidence.Gain(spoils.Confidence.Max);
            Debug.Log("ALICE_AUTOMATION confidence filled.");
        }

        [MenuItem("Alice/Automation/Press Heal")]
        public static void PressHeal()
        {
            TapButton(Keyboard.current?.qKey);
        }

        [MenuItem("Alice/Automation/Press Map")]
        public static void PressMap()
        {
            TapButton(Keyboard.current?.mKey);
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

            PlayerMotor alice = Object.FindAnyObjectByType<PlayerMotor>();
            if (alice != null && alice.TryGetComponent(out IPlayerCommandSource commands))
            {
                report.Append($"alice x={alice.Position.x:0.##} facing={alice.Facing} move={commands.Move} locked={alice.IsLocked}");
            }

            Debug.Log(report.ToString());
        }

        /// <summary>Starts the prototype rooms from scratch: no rest, no decrees, no lost purse.</summary>
        [MenuItem("Alice/Automation/Delete Save")]
        public static void DeleteSave()
        {
            // The slot the game plays in: slot 1 unless the main menu chose another this session.
            Alice.Save.SaveStore store = Alice.Save.SaveSlots.Store(Alice.Save.SaveSlots.Current);
            store.Delete();
            Debug.Log("ALICE_AUTOMATION deleted the save at " + store.FilePath);
        }

        /// <summary>Gives Alice decrees as if she had beaten enemies, to try the lost purse quickly.</summary>
        [MenuItem("Alice/Automation/Give 50 Decrees")]
        public static void GiveDecrees()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("ALICE_AUTOMATION works in Play mode only, so the scene is never changed.");
                return;
            }

            PlayerSpoils spoils = Object.FindAnyObjectByType<PlayerSpoils>();
            if (spoils != null)
            {
                EnemyDefeats.Raise(new EnemyDefeat(null, new HitInfo(1, 1, spoils.gameObject), 50, 0f));
                Debug.Log($"ALICE_AUTOMATION decrees now {spoils.Decrees}.");
            }
        }

        [MenuItem("Alice/Automation/Reset Enemies")]
        public static void ResetEnemies()
        {
            RouteInput();
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("ALICE_AUTOMATION works in Play mode only, so the scene is never changed.");
                return;
            }

            // What a rest will do in M3: everyone back at the post, alive and hostile.
            MeleeEnemy[] enemies = Object.FindObjectsByType<MeleeEnemy>();
            foreach (MeleeEnemy enemy in enemies)
            {
                enemy.ResetToHome();
            }

            EnemyBase[] others = Object.FindObjectsByType<EnemyBase>();
            foreach (EnemyBase enemy in others)
            {
                enemy.ResetToHome();
            }

            Debug.Log($"ALICE_AUTOMATION reset {enemies.Length + others.Length} enemies.");
        }

        /// <summary>Throws the nearest enemy open as a parry would; the narrow windows are hard to hit through menu calls.</summary>
        [MenuItem("Alice/Automation/Open Nearest Enemy")]
        public static void OpenNearestEnemy()
        {
            RouteInput();
            PlayerMotor alice = Object.FindAnyObjectByType<PlayerMotor>();
            MeleeEnemy nearest = EditorApplication.isPlaying && alice != null ? FindNearestEnemy(alice.Position.x) : null;
            if (nearest == null)
            {
                Debug.LogWarning("ALICE_AUTOMATION found no living enemy in Play mode.");
                return;
            }

            nearest.OnParried(alice.gameObject);
            Debug.Log($"ALICE_AUTOMATION opened {nearest.name}.");
        }

        /// <summary>Wounds the nearest enemy down to its stun threshold, as Alice's blows would, so the execution is on offer.</summary>
        [MenuItem("Alice/Automation/Stun Nearest Enemy")]
        public static void StunNearestEnemy()
        {
            RouteInput();
            PlayerMotor alice = Object.FindAnyObjectByType<PlayerMotor>();
            MeleeEnemy nearest = EditorApplication.isPlaying && alice != null ? FindNearestEnemy(alice.Position.x) : null;
            if (nearest == null)
            {
                Debug.LogWarning("ALICE_AUTOMATION found no living enemy in Play mode.");
                return;
            }

            Health health = nearest.GetComponent<Health>();
            int damage = Mathf.Max(1, health.Current - Mathf.Max(1, nearest.Settings.StunAtHealth));
            int side = nearest.transform.position.x >= alice.Position.x ? 1 : -1;
            health.ReceiveHit(new HitInfo(damage, side, alice.gameObject, HitKind.Thrust));
            Debug.Log($"ALICE_AUTOMATION hit {nearest.name} for {damage}; state {nearest.CurrentState}.");
        }

        [MenuItem("Alice/Automation/Defeat Alice")]
        public static void DefeatAlice()
        {
            RouteInput();
            PlayerMotor alice = Object.FindAnyObjectByType<PlayerMotor>();
            if (!EditorApplication.isPlaying || alice == null)
            {
                Debug.LogWarning("ALICE_AUTOMATION needs Alice in Play mode.");
                return;
            }

            alice.GetComponent<Health>().Kill(new HitInfo(1, -alice.Facing, alice.gameObject));
            Debug.Log("ALICE_AUTOMATION Alice defeated.");
        }

        private static MeleeEnemy FindNearestEnemy(float x)
        {
            MeleeEnemy nearest = null;
            float best = float.MaxValue;
            foreach (MeleeEnemy enemy in Object.FindObjectsByType<MeleeEnemy>())
            {
                float distance = Mathf.Abs(enemy.transform.position.x - x);
                if (enemy.CurrentState != MeleeEnemy.State.Dead && distance < best)
                {
                    best = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        [MenuItem("Alice/Automation/Put Alice Beside Nearest Enemy")]
        public static void PutAliceBesideNearestEnemy()
        {
            RouteInput();
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

            Transform nearest = null;
            int facing = 1;
            float floorY = 0f;
            float best = float.MaxValue;
            foreach (MeleeEnemy enemy in Object.FindObjectsByType<MeleeEnemy>())
            {
                float distance = Mathf.Abs(enemy.transform.position.x - alice.Position.x);
                if (enemy.CurrentState != MeleeEnemy.State.Dead && distance < best)
                {
                    best = distance;
                    nearest = enemy.transform;
                    facing = enemy.Facing;
                    floorY = enemy.transform.position.y;
                }
            }

            // The biome 1 archetypes; a flyer puts her on the floor under it, not in the air beside it.
            foreach (EnemyBase enemy in Object.FindObjectsByType<EnemyBase>())
            {
                float distance = Mathf.Abs(enemy.transform.position.x - alice.Position.x);
                if (!enemy.IsDead && distance < best)
                {
                    best = distance;
                    nearest = enemy.transform;
                    facing = enemy.Facing;
                    floorY = enemy is FlyingDiver flyer ? flyer.FloorY : enemy.transform.position.y;
                }
            }

            if (nearest == null)
            {
                Debug.LogWarning("ALICE_AUTOMATION found no living enemy.");
                return;
            }

            // On the side the enemy faces, so it notices her at once and she faces it after one step.
            Vector2 target = new Vector2(nearest.position.x + facing * BesideEnemyDistance, floorY + 0.02f);
            alice.Teleport(target);
            alice.Face(-facing);

            // The follow camera eases over seconds, longer still under a slowed time scale; jump it there.
            Camera camera = Camera.main;
            if (camera != null && camera.TryGetComponent(out CameraFollow2D _))
            {
                camera.transform.position = new Vector3(target.x + 0.5f * facing * -BesideEnemyDistance,
                    target.y + CameraHeight, camera.transform.position.z);
            }

            Debug.Log($"ALICE_AUTOMATION Alice placed at {target.x:0.##} facing {alice.Facing} beside {nearest.name} facing {facing}.");
        }
    }
}
