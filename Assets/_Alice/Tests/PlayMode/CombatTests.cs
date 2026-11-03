using System.Collections;
using System.Collections.Generic;
using Alice.Combat;
using Alice.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Alice.Tests
{
    public sealed class CombatTests
    {
        private readonly List<Object> _created = new List<Object>();
        private ScriptedCommands _commands;
        private PlayerMotor _motor;
        private Health _aliceHealth;
        private MeleeEnemySettings _enemySettings;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameObject floor = Track(new GameObject("Floor"));
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(100f, 1f);

            PlayerMovementSettings movement = Track(ScriptableObject.CreateInstance<PlayerMovementSettings>());
            PlayerCombatSettings combat = Track(ScriptableObject.CreateInstance<PlayerCombatSettings>());
            _enemySettings = Track(ScriptableObject.CreateInstance<MeleeEnemySettings>());

            GameObject alice = Track(new GameObject("TestAlice"));
            alice.SetActive(false);
            alice.transform.position = new Vector3(0f, 0.02f, 0f);
            alice.AddComponent<Rigidbody2D>();
            alice.AddComponent<BoxCollider2D>();
            _commands = alice.AddComponent<ScriptedCommands>();
            _motor = alice.AddComponent<PlayerMotor>();
            _motor.Settings = movement;
            _aliceHealth = alice.AddComponent<Health>();
            alice.AddComponent<PlayerAttack>().Bind(combat, null);
            alice.AddComponent<PlayerDamageReaction>().Settings = combat;
            alice.SetActive(true);

            yield return Steps(10);
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
        public IEnumerator RepeatedThrusts_StaggerAndKillSoldier()
        {
            MeleeEnemy soldier = CreateSoldier(2f, -1);
            Health soldierHealth = soldier.GetComponent<Health>();

            float timeout = Time.time + 3f;
            while (!soldierHealth.IsDead && Time.time < timeout)
            {
                _commands.AttackPressedAt = Time.time;
                yield return new WaitForFixedUpdate();
            }

            Assert.That(soldierHealth.IsDead, Is.True, "Thrusts in range should kill the soldier.");
            Assert.That(soldier.CurrentState, Is.EqualTo(MeleeEnemy.State.Dead));
            Assert.That(_aliceHealth.Current, Is.EqualTo(_aliceHealth.Max), "A staggered soldier must not land its blow.");
        }

        [UnityTest]
        public IEnumerator SoldierLunge_HurtsAndPushesAliceAway()
        {
            CreateSoldier(2.5f, -1);

            float timeout = Time.time + 3f;
            while (_aliceHealth.Current == _aliceHealth.Max && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(_aliceHealth.Current, Is.EqualTo(_aliceHealth.Max - 1), "The lunge should land once.");
            yield return Steps(8);
            Assert.That(_motor.Position.x, Is.LessThan(-0.5f), "Knockback pushes Alice away from the blow.");
        }

        private MeleeEnemy CreateSoldier(float x, int facing)
        {
            GameObject body = Track(new GameObject("TestSoldier"));
            body.SetActive(false);
            body.transform.position = new Vector3(x, 0f, 0f);
            MeleeEnemy enemy = body.AddComponent<MeleeEnemy>();
            enemy.Settings = _enemySettings;
            enemy.InitialFacing = facing;
            body.SetActive(true);
            return enemy;
        }

        private static IEnumerator Steps(int count)
        {
            for (int index = 0; index < count; index++)
            {
                yield return new WaitForFixedUpdate();
            }
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
        }
    }
}
