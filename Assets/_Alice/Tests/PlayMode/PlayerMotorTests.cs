using System.Collections;
using System.Collections.Generic;
using Alice.Abilities;
using Alice.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Alice.Tests
{
    public sealed class PlayerMotorTests
    {
        private readonly List<Object> _created = new List<Object>();
        private PlayerMovementSettings _settings;
        private ScriptedCommands _commands;
        private PlayerMotor _motor;
        private AbilityInventory _abilities;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _settings = Track(ScriptableObject.CreateInstance<PlayerMovementSettings>());
            CreateSolid("Floor", new Vector2(-50f, -1f), new Vector2(150f, 0f));

            GameObject alice = Track(new GameObject("TestAlice"));
            alice.SetActive(false);
            alice.transform.position = new Vector3(0f, 0.02f, 0f);
            alice.AddComponent<Rigidbody2D>();
            alice.AddComponent<BoxCollider2D>();
            _commands = alice.AddComponent<ScriptedCommands>();
            _abilities = alice.AddComponent<AbilityInventory>();
            _motor = alice.AddComponent<PlayerMotor>();
            _motor.Settings = _settings;
            alice.SetActive(true);

            yield return Steps(10);
            Assert.That(_motor.IsGrounded, Is.True, "Alice should settle on the floor.");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.Destroy(created);
            }

            _created.Clear();
        }

        [UnityTest]
        public IEnumerator HeldJump_ReachesConfiguredHeight()
        {
            _commands.PressJump();
            float apex = 0f;
            yield return TrackApex(1.2f, value => apex = value);

            Assert.That(apex, Is.EqualTo(TraversalMetrics.JumpHeight).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator TappedJump_IsMuchLower()
        {
            _commands.PressJump();
            yield return Steps(3);
            _commands.JumpHeld = false;
            float apex = 0f;
            yield return TrackApex(1.2f, value => apex = value);

            Assert.That(apex, Is.LessThan(TraversalMetrics.JumpHeight * 0.6f));
        }

        [UnityTest]
        public IEnumerator Run_ReachesMaxSpeedQuickly()
        {
            _commands.Move = Vector2.right;
            yield return Steps(15);

            Assert.That(_motor.Velocity.x, Is.EqualTo(TraversalMetrics.MaxRunSpeed).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator FanJump_RequiresAbilityAndAddsHeight()
        {
            float apex = 0f;
            _commands.PressJump();
            yield return WaitForFalling();
            _commands.PressJump();
            yield return TrackApex(1.5f, value => apex = value);
            Assert.That(apex, Is.LessThan(TraversalMetrics.JumpHeight + 0.1f), "No fan jump without A01.");

            _abilities.Unlock(AbilityIds.Fan);
            _commands.PressJump();
            yield return WaitForFalling();
            _commands.PressJump();
            yield return TrackApex(1.5f, value => apex = value);
            Assert.That(apex, Is.EqualTo(TraversalMetrics.JumpHeight + TraversalMetrics.FanJumpHeight).Within(0.2f));
        }

        [UnityTest]
        public IEnumerator Slide_PassesUnderLowCeiling()
        {
            CreateSolid("LowCeiling", new Vector2(2f, 1.5f), new Vector2(10f, 4f));
            _commands.Move = Vector2.right;
            _commands.EvadePressedAt = Time.time;
            yield return Steps(5);
            _commands.Move = Vector2.zero;

            float timeout = Time.time + 3f;
            while (_motor.IsSliding && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(_motor.IsSliding, Is.False, "Slide should end after the tunnel.");
            Assert.That(_motor.Position.x, Is.GreaterThan(10f), "Alice must be past the ceiling to stand up.");
        }

        [UnityTest]
        public IEnumerator WalkOffLedge_NoJumpAfterCoyoteTime()
        {
            CreateSolid("Ledge", new Vector2(-3f, 3f), new Vector2(1f, 5f));
            _motor.Teleport(new Vector2(0f, 5.02f));
            yield return Steps(10);
            Assert.That(_motor.IsGrounded, Is.True, "Alice should stand on the ledge.");

            _commands.Move = Vector2.right;
            while (_motor.IsGrounded)
            {
                yield return new WaitForFixedUpdate();
            }

            yield return new WaitForSeconds(_settings.CoyoteTime + 0.05f);
            _commands.PressJump();
            yield return Steps(3);

            Assert.That(_motor.Velocity.y, Is.LessThan(0f), "Jump must not fire after coyote time.");
        }

        [UnityTest]
        public IEnumerator FallingAlongWall_CannotJump()
        {
            CreateSolid("Wall", new Vector2(2f, 0f), new Vector2(3f, 12f));
            _motor.Teleport(new Vector2(2f - TraversalMetrics.StandingSize.x * 0.5f - 0.02f, 8f));
            _commands.Move = Vector2.right;
            yield return Steps(8);

            _commands.PressJump();
            yield return Steps(3);

            Assert.That(_motor.IsGrounded, Is.False, "A wall is not ground.");
            Assert.That(_motor.Velocity.y, Is.LessThan(0f), "Jump must not fire against a wall mid-air.");
        }

        private IEnumerator TrackApex(float duration, System.Action<float> report)
        {
            float apex = _motor.Position.y;
            float end = Time.time + duration;
            while (Time.time < end)
            {
                yield return new WaitForFixedUpdate();
                apex = Mathf.Max(apex, _motor.Position.y);
                if (_motor.IsGrounded && _motor.Velocity.y <= 0f && apex > 0.5f)
                {
                    break;
                }
            }

            report(apex);
            _commands.JumpHeld = false;
            yield return Steps(5);
        }

        private IEnumerator WaitForFalling()
        {
            yield return Steps(2);
            while (_motor.Velocity.y > 0f)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        private static IEnumerator Steps(int count)
        {
            for (int index = 0; index < count; index++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        private void CreateSolid(string name, Vector2 min, Vector2 max)
        {
            GameObject solid = Track(new GameObject(name));
            solid.transform.position = (min + max) * 0.5f;
            solid.AddComponent<BoxCollider2D>().size = max - min;
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }

        private sealed class ScriptedCommands : MonoBehaviour, IPlayerCommandSource
        {
            public Vector2 Move { get; set; }
            public bool JumpHeld { get; set; }
            public float JumpPressedAt { get; set; } = float.NegativeInfinity;
            public float EvadePressedAt { get; set; } = float.NegativeInfinity;
            public float AttackPressedAt { get; set; } = float.NegativeInfinity;

            public void PressJump()
            {
                JumpPressedAt = Time.time;
                JumpHeld = true;
            }
        }
    }
}
