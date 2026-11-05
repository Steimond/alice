using System.Collections;
using System.Collections.Generic;
using Alice.Combat;
using Alice.Core;
using Alice.Player;
using Alice.Save;
using Alice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Alice.Tests
{
    public sealed class RestAndLossTests
    {
        private readonly List<Object> _created = new List<Object>();
        private ScriptedCommands _commands;
        private PlayerMotor _motor;
        private Health _health;
        private PlayerRest _rest;
        private PlayerSpoils _spoils;
        private PlayerCombatSettings _combat;
        private MeleeEnemySettings _enemySettings;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameObject floor = Track(new GameObject("Floor"));
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(100f, 1f);

            PlayerMovementSettings movement = Track(ScriptableObject.CreateInstance<PlayerMovementSettings>());
            _combat = Track(ScriptableObject.CreateInstance<PlayerCombatSettings>());
            _enemySettings = Track(ScriptableObject.CreateInstance<MeleeEnemySettings>());

            GameObject alice = Track(new GameObject("TestAlice"));
            alice.SetActive(false);
            alice.transform.position = new Vector3(0f, 0.02f, 0f);
            alice.AddComponent<Rigidbody2D>();
            alice.AddComponent<BoxCollider2D>();
            _commands = alice.AddComponent<ScriptedCommands>();
            _motor = alice.AddComponent<PlayerMotor>();
            _motor.Settings = movement;
            _health = alice.AddComponent<Health>();
            alice.AddComponent<PlayerRespawn>();
            alice.AddComponent<PlayerAttack>().Bind(_combat);
            alice.AddComponent<PlayerParry>().Settings = _combat;
            alice.AddComponent<PlayerDamageReaction>().Settings = _combat;
            alice.AddComponent<PlayerSalts>().Settings = _combat;
            _spoils = alice.AddComponent<PlayerSpoils>();
            _spoils.Settings = _combat;
            alice.AddComponent<PlayerLoadout>();
            _rest = alice.AddComponent<PlayerRest>();
            _rest.Settings = _combat;
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
        public IEnumerator Rest_RestoresClarityResetsEnemiesAndBecomesTheWakePoint()
        {
            RestPoint chair = CreateChair(new Vector2(-6f, 0f), "R_TEST");
            MeleeEnemy soldier = CreateSoldier(12f);
            yield return Steps(2);
            soldier.GetComponent<Health>().Kill(new HitInfo(99, 1, _motor.gameObject));
            _health.ReceiveHit(new HitInfo(30, -1, null));
            yield return new WaitForSeconds(0.5f);

            _motor.Teleport(new Vector2(-5.5f, 0.02f));
            yield return Steps(5);
            _commands.InteractPressedAt = Time.time;
            yield return new WaitForSeconds(_combat.RestSitTime + 0.2f);

            Assert.That(_rest.Stage, Is.EqualTo(PlayerRest.RestStage.Resting));
            Assert.That(_health.Current, Is.EqualTo(_health.Max), "Rest restores clarity.");
            Assert.That(soldier.CurrentState, Is.Not.EqualTo(MeleeEnemy.State.Dead), "Rest brings ordinary enemies back.");
            Assert.That(_rest.LastRestId, Is.EqualTo("R_TEST"));
            Assert.That(_motor.Position.x, Is.EqualTo(chair.SeatPosition.x).Within(0.01f));

            _commands.JumpPressedAt = Time.time;
            yield return new WaitForSeconds(_combat.RestRiseTime + 0.2f);
            Assert.That(_rest.Stage, Is.EqualTo(PlayerRest.RestStage.None), "Jump gets her up.");

            _motor.Teleport(new Vector2(4f, 0.02f));
            yield return Steps(3);
            _health.Kill(new HitInfo(1, 1, null));
            yield return WaitForWake();
            Assert.That(_motor.Position.x, Is.EqualTo(chair.SeatPosition.x).Within(0.05f), "A defeat wakes her in the chair.");
        }

        [UnityTest]
        public IEnumerator Defeat_LeavesDecreesWhereSheFell_InteractNextToThemGivesThemBack()
        {
            CreateCache();
            Earn(25);
            _motor.Teleport(new Vector2(5f, 0.02f));
            yield return Steps(10);
            Vector2 fell = _motor.Position;
            _health.Kill(new HitInfo(1, 1, null));
            yield return WaitForWake();

            Assert.That(_spoils.Decrees, Is.EqualTo(0), "She wakes with nothing in her pocket.");
            Assert.That(_spoils.HasLoss, Is.True);
            Assert.That(_spoils.LossAmount, Is.EqualTo(25));
            Assert.That(_spoils.LossPosition.x, Is.EqualTo(fell.x).Within(0.1f), "The purse lies at her last safe footing.");

            _motor.Teleport(_spoils.LossPosition + new Vector2(0.5f, 0.02f));
            yield return Steps(5);
            Assert.That(_spoils.HasLoss, Is.True, "Standing by the purse does not pick it up.");

            _commands.InteractPressedAt = Time.time;
            yield return Steps(3);
            Assert.That(_spoils.Decrees, Is.EqualTo(25), "Interact by the purse returns all of it.");
            Assert.That(_spoils.HasLoss, Is.False);
        }

        [UnityTest]
        public IEnumerator SecondDefeat_BeforePickingUp_LosesTheOldPurseForGood()
        {
            Earn(25);
            _motor.Teleport(new Vector2(5f, 0.02f));
            yield return Steps(10);
            _health.Kill(new HitInfo(1, 1, null));
            yield return WaitForWake();

            Earn(7);
            _motor.Teleport(new Vector2(-5f, 0.02f));
            yield return Steps(10);
            _health.Kill(new HitInfo(1, 1, null));
            yield return WaitForWake();

            Assert.That(_spoils.LossAmount, Is.EqualTo(7), "Only what she earned since waking lies on the ground now.");
            Assert.That(_spoils.LossPosition.x, Is.EqualTo(-5f).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator SaveData_CarriesDecreesLossTechniqueAndRest()
        {
            CreateChair(new Vector2(-6f, 0f), "R_TEST");
            Earn(12);
            yield return Steps(2);
            SaveData saved = new SaveData { RestId = "R_TEST" };
            foreach (ISaveParticipant participant in _motor.GetComponents<ISaveParticipant>())
            {
                participant.Capture(saved);
            }

            saved.RestId = "R_TEST";
            saved.Loss = new DecreeLossData { Exists = true, RoomId = "P_TEST", X = 2f, Y = 0f, Amount = 9 };
            saved.EquippedTechnique = string.Empty;
            foreach (ISaveParticipant participant in _motor.GetComponents<ISaveParticipant>())
            {
                participant.Restore(saved);
            }

            yield return Steps(2);
            Assert.That(_spoils.Decrees, Is.EqualTo(12));
            Assert.That(_spoils.HasLoss, Is.True);
            Assert.That(_spoils.LossRoomId, Is.EqualTo("P_TEST"));
            Assert.That(_motor.GetComponent<PlayerLoadout>().EquippedTechnique, Is.Empty);
            Assert.That(_rest.LastRestId, Is.EqualTo("R_TEST"), "Loading starts her in the chair it names.");
            Assert.That(_motor.Position.x, Is.EqualTo(-6f).Within(0.05f));
        }

        private void Earn(int decrees)
        {
            EnemyDefeats.Raise(new EnemyDefeat(null, new HitInfo(1, 1, _motor.gameObject), decrees, 0f));
        }

        private IEnumerator WaitForWake()
        {
            PlayerDamageReaction reaction = _motor.GetComponent<PlayerDamageReaction>();
            yield return Steps(2);
            float timeout = Time.time + 6f;
            while (reaction.IsDefeated && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        private RestPoint CreateChair(Vector2 position, string restId)
        {
            GameObject chair = Track(new GameObject("TestChair"));
            chair.SetActive(false);
            chair.transform.position = position;
            BoxCollider2D zone = chair.AddComponent<BoxCollider2D>();
            zone.isTrigger = true;
            zone.size = new Vector2(3f, 3f);
            zone.offset = new Vector2(0f, 1.5f);
            RestPoint rest = chair.AddComponent<RestPoint>();
            rest.RestId = restId;
            chair.SetActive(true);
            return rest;
        }

        private void CreateCache()
        {
            Track(new GameObject("TestCache")).AddComponent<DecreeCache>();
        }

        private MeleeEnemy CreateSoldier(float x)
        {
            GameObject body = Track(new GameObject("TestSoldier"));
            body.SetActive(false);
            body.transform.position = new Vector3(x, 0f, 0f);
            MeleeEnemy enemy = body.AddComponent<MeleeEnemy>();
            enemy.Settings = _enemySettings;
            enemy.InitialFacing = -1;
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
            public float ParryPressedAt { get; set; } = float.NegativeInfinity;
            public float InteractPressedAt { get; set; } = float.NegativeInfinity;
            public float HealPressedAt { get; set; } = float.NegativeInfinity;
            public float UseAbilityPressedAt { get; set; } = float.NegativeInfinity;
        }
    }
}
