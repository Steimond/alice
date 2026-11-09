using System.Collections;
using System.Collections.Generic;
using Alice.Combat;
using Alice.Core;
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
            // The stun is a chance in the game; these tests need it every time.
            typeof(MeleeEnemySettings).GetField("_stunChance", System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic).SetValue(_enemySettings, 1f);

            GameObject alice = Track(new GameObject("TestAlice"));
            alice.SetActive(false);
            alice.transform.position = new Vector3(0f, 0.02f, 0f);
            alice.AddComponent<Rigidbody2D>();
            alice.AddComponent<BoxCollider2D>();
            _commands = alice.AddComponent<ScriptedCommands>();
            _motor = alice.AddComponent<PlayerMotor>();
            _motor.Settings = movement;
            _aliceHealth = alice.AddComponent<Health>();
            alice.AddComponent<PlayerAttack>().Bind(combat);
            alice.AddComponent<PlayerParry>().Settings = combat;
            alice.AddComponent<PlayerDamageReaction>().Settings = combat;
            alice.AddComponent<PlayerSalts>().Settings = combat;
            alice.AddComponent<PlayerSpoils>().Settings = combat;
            alice.AddComponent<PlayerLoadout>();
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
        public IEnumerator KillingSoldier_GivesDecreesAndConfidence_ExecutionGivesMore()
        {
            PlayerSpoils spoils = _motor.GetComponent<PlayerSpoils>();
            MeleeEnemy first = CreateSoldier(6f, -1);
            first.GetComponent<Health>().Kill(new Alice.Core.HitInfo(1, 1, _motor.gameObject, Alice.Core.HitKind.Thrust));
            yield return Steps(1);

            Assert.That(spoils.Decrees, Is.EqualTo(_enemySettings.DecreeReward));
            float plain = spoils.Confidence.Current;
            Assert.That(plain, Is.EqualTo(_enemySettings.ConfidenceReward).Within(1e-4f));

            MeleeEnemy second = CreateSoldier(-6f, 1);
            second.GetComponent<Health>().Kill(new Alice.Core.HitInfo(1, 1, _motor.gameObject, Alice.Core.HitKind.Execution));
            yield return Steps(1);
            Assert.That(spoils.Confidence.Current - plain, Is.GreaterThan(_enemySettings.ConfidenceReward),
                "An execution gives more confidence than a plain kill.");

            MeleeEnemy third = CreateSoldier(10f, -1);
            third.GetComponent<Health>().Kill(new Alice.Core.HitInfo(1, 1, third.gameObject));
            yield return Steps(1);
            Assert.That(spoils.Decrees, Is.EqualTo(2 * _enemySettings.DecreeReward), "Only Alice's kills count.");
        }

        [UnityTest]
        public IEnumerator Salts_RestoreClarityAndSpendACharge_HitWastesIt()
        {
            PlayerSalts salts = _motor.GetComponent<PlayerSalts>();
            int charges = salts.Charges;
            _aliceHealth.ReceiveHit(new Alice.Core.HitInfo(60, -1, null));
            int wounded = _aliceHealth.Current;
            yield return Steps(30);

            _commands.HealPressedAt = Time.time;
            yield return Steps(2);
            Assert.That(salts.IsUsing, Is.True);
            Assert.That(salts.Charges, Is.EqualTo(charges - 1));

            float timeout = Time.time + 2f;
            while (salts.IsUsing && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(_aliceHealth.Current, Is.GreaterThan(wounded), "The sniff brings clarity back.");

            int beforeSecond = _aliceHealth.Current;
            _commands.HealPressedAt = Time.time;
            yield return Steps(2);
            Assert.That(salts.IsUsing, Is.True);
            yield return Steps(15);
            _aliceHealth.ReceiveHit(new Alice.Core.HitInfo(10, -1, null));
            yield return Steps(40);
            Assert.That(salts.IsUsing, Is.False);
            Assert.That(salts.Charges, Is.EqualTo(charges - 2), "The interrupted use still spent its charge.");
            Assert.That(_aliceHealth.Current, Is.EqualTo(beforeSecond - 10), "A hit before the sniff wastes it.");
        }

        [UnityTest]
        public IEnumerator SpinStrike_CostsConfidence_HitsBothSidesAndBeheads()
        {
            PlayerSpoils spoils = _motor.GetComponent<PlayerSpoils>();
            PlayerAttack attack = _motor.GetComponent<PlayerAttack>();
            _commands.UseAbilityPressedAt = Time.time;
            yield return Steps(3);
            Assert.That(attack.IsAttacking, Is.False, "Without enough confidence the technique does not start.");

            spoils.Confidence.Gain(100f);
            MeleeEnemy front = CreateSoldier(_motor.Position.x + 1.6f, -1);
            MeleeEnemy behind = CreateSoldier(_motor.Position.x - 1.6f, 1);
            yield return Steps(2);
            _commands.UseAbilityPressedAt = Time.time;
            yield return Steps(2);
            Assert.That(attack.Strike, Is.EqualTo(PlayerStrike.Technique));
            Assert.That(attack.TechniqueId, Is.EqualTo(TechniqueIds.SpinStrike));
            Assert.That(spoils.Confidence.Current, Is.LessThan(100f), "The technique is paid for at once.");

            float timeout = Time.time + 2f;
            while ((front.CurrentState != MeleeEnemy.State.Dead || behind.CurrentState != MeleeEnemy.State.Dead)
                && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(front.CurrentState, Is.EqualTo(MeleeEnemy.State.Dead));
            Assert.That(behind.CurrentState, Is.EqualTo(MeleeEnemy.State.Dead), "The spin reaches behind her too.");
            Assert.That(front.KillingBlow, Is.EqualTo(Alice.Core.HitKind.Spin), "A killing spin beheads.");
            Assert.That(behind.Facing, Is.EqualTo(1), "The one behind falls away from her, facing her back.");
        }

        [UnityTest]
        public IEnumerator Salts_AtFullClarity_StillSpendTheCharge()
        {
            PlayerSalts salts = _motor.GetComponent<PlayerSalts>();
            int charges = salts.Charges;
            _commands.HealPressedAt = Time.time;
            yield return Steps(2);

            Assert.That(salts.IsUsing, Is.True, "A misclick at full clarity is not ignored.");
            Assert.That(salts.Charges, Is.EqualTo(charges - 1));
            Assert.That(_aliceHealth.Current, Is.EqualTo(_aliceHealth.Max));
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

            Assert.That(_aliceHealth.Current, Is.EqualTo(_aliceHealth.Max - _enemySettings.Damage), "The lunge should land once.");
            yield return Steps(8);
            Assert.That(_motor.Position.x, Is.LessThan(-0.5f), "Knockback pushes Alice away from the blow.");
        }

        [UnityTest]
        public IEnumerator Thrust_DrawsFramesOfEachPhase()
        {
            Texture2D texture = Track(new Texture2D(4, 4));
            Sprite Frame(string name)
            {
                Sprite sprite = Track(Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), Vector2.zero));
                sprite.name = name;
                return sprite;
            }

            Sprite idle = Frame("idle");
            Sprite windup = Frame("windup");
            Sprite active = Frame("active");
            Sprite recovery = Frame("recovery");
            PlayerSpriteSet sprites = Track(ScriptableObject.CreateInstance<PlayerSpriteSet>());
            sprites.Assign(idle, new[] { idle });
            sprites.AssignThrust(new[] { windup }, new[] { active }, new[] { recovery });

            SpriteRenderer renderer = Track(new GameObject("TestAliceSprite")).AddComponent<SpriteRenderer>();
            _motor.gameObject.AddComponent<PlayerSpriteView>().Bind(_motor, sprites, renderer, null, null);
            PlayerAttack attack = _motor.GetComponent<PlayerAttack>();

            _commands.AttackPressedAt = Time.time;
            var seen = new HashSet<Sprite>();
            float timeout = Time.time + 1f;
            while (Time.time < timeout && (seen.Count < 3 || attack.IsAttacking))
            {
                yield return null;
                seen.Add(renderer.sprite);
            }

            // The strike ends in a physics step; the view catches up in the next drawn frame.
            yield return null;
            Assert.That(seen, Is.SupersetOf(new[] { windup, active, recovery }));
            Assert.That(renderer.sprite, Is.EqualTo(idle), "Alice returns to standing after the thrust.");
        }

        [UnityTest]
        public IEnumerator SecondPress_ChainsThrustIntoSlash()
        {
            PlayerAttack attack = _motor.GetComponent<PlayerAttack>();
            _commands.AttackPressedAt = Time.time;
            float timeout = Time.time + 1f;
            while (attack.Phase != Alice.Combat.AttackPhase.Active && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(attack.Strike, Is.EqualTo(PlayerStrike.Thrust));
            _commands.AttackPressedAt = Time.time;
            while (attack.Strike == PlayerStrike.Thrust && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(attack.Strike, Is.EqualTo(PlayerStrike.Slash), "A press during the thrust chains the slash.");
        }

        [UnityTest]
        public IEnumerator ParryCounterThenInteract_StunsAndExecutesSoldier()
        {
            MeleeEnemy soldier = CreateSoldier(2.5f, -1);
            float windup = _enemySettings.Timeline.Windup;
            float timeout = Time.time + 3f;
            while (!(soldier.CurrentState == MeleeEnemy.State.Attack && soldier.StateTime >= windup - 0.08f)
                && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            _commands.ParryPressedAt = Time.time;
            while (soldier.CurrentState != MeleeEnemy.State.Opened && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(soldier.CurrentState, Is.EqualTo(MeleeEnemy.State.Opened), "The parried soldier is thrown open.");
            Assert.That(_aliceHealth.Current, Is.EqualTo(_aliceHealth.Max), "A parried blow does no damage.");

            Assert.That(soldier.CanBeExecuted, Is.False, "A parry alone does not offer the execution.");

            yield return Steps(10);
            _commands.AttackPressedAt = Time.time;
            while (soldier.CurrentState != MeleeEnemy.State.Stunned && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(soldier.CurrentState, Is.EqualTo(MeleeEnemy.State.Stunned), "The counter leaves the soldier nearly beaten and stunned.");
            Assert.That(soldier.CanBeExecuted, Is.True);

            while (_motor.GetComponent<PlayerAttack>().IsAttacking && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            _commands.InteractPressedAt = Time.time;
            while (soldier.CurrentState != MeleeEnemy.State.Dead && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(soldier.CurrentState, Is.EqualTo(MeleeEnemy.State.Dead), "Interact next to a stunned soldier executes it.");
            Assert.That(soldier.KillingBlow, Is.EqualTo(Alice.Core.HitKind.Execution));
            Assert.That(_motor.GetComponent<PlayerAttack>().Strike, Is.EqualTo(PlayerStrike.Execution));
            Assert.That(_aliceHealth.Current, Is.EqualTo(_aliceHealth.Max), "Alice is safe during the execution.");
        }

        [UnityTest]
        public IEnumerator StunnedSoldier_CanBeFinishedWithoutExecution()
        {
            MeleeEnemy soldier = CreateSoldier(2f, -1);
            Health soldierHealth = soldier.GetComponent<Health>();
            yield return Steps(2);

            soldierHealth.ReceiveHit(new Alice.Core.HitInfo(soldierHealth.Current - 1, 1, _motor.gameObject,
                Alice.Core.HitKind.Thrust));
            yield return Steps(1);
            Assert.That(soldier.CurrentState, Is.EqualTo(MeleeEnemy.State.Stunned), "One health left stuns the soldier.");

            yield return Steps(10);
            _motor.Face(1);
            _commands.AttackPressedAt = Time.time;
            float timeout = Time.time + 2f;
            while (soldier.CurrentState != MeleeEnemy.State.Dead && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(soldier.CurrentState, Is.EqualTo(MeleeEnemy.State.Dead), "Any blow still finishes a stunned enemy.");
            Assert.That(soldier.KillingBlow, Is.EqualTo(Alice.Core.HitKind.Thrust), "Attack never starts an execution.");
        }

        [UnityTest]
        public IEnumerator Defeat_FallsDarkensAndWakesAtStartWithEnemiesReset()
        {
            Vector2 start = _motor.Position;
            MeleeEnemy soldier = CreateSoldier(start.x + 8f, -1);
            PlayerDamageReaction reaction = _motor.GetComponent<PlayerDamageReaction>();
            yield return Steps(2);

            soldier.GetComponent<Health>().Kill(new Alice.Core.HitInfo(1, 1, _motor.gameObject, Alice.Core.HitKind.Thrust));
            _motor.Teleport(start + new Vector2(2f, 0f));
            yield return Steps(2);
            Assert.That(soldier.CurrentState, Is.EqualTo(MeleeEnemy.State.Dead));

            _aliceHealth.Kill(new Alice.Core.HitInfo(1, -1, soldier.gameObject));
            Assert.That(reaction.Stage, Is.EqualTo(PlayerDamageReaction.DefeatStage.Falling));
            Assert.That(reaction.Darkness, Is.EqualTo(0f), "The screen darkens only toward the end of the fall.");

            float timeout = Time.time + 6f;
            while (reaction.Stage != PlayerDamageReaction.DefeatStage.Dark && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(reaction.Darkness, Is.EqualTo(1f));
            Assert.That(_motor.Position.x, Is.EqualTo(start.x).Within(0.05f), "Alice wakes where the room started her.");
            Assert.That(_aliceHealth.Current, Is.EqualTo(_aliceHealth.Max));
            Assert.That(soldier.CurrentState, Is.Not.EqualTo(MeleeEnemy.State.Dead), "A defeat resets the enemies like a rest.");
            Assert.That(_aliceHealth.ReceiveHit(new Alice.Core.HitInfo(1, -1, soldier.gameObject)), Is.False,
                "Nothing hurts her while she lies there or gets up.");

            while (reaction.Stage != PlayerDamageReaction.DefeatStage.None && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(reaction.Stage, Is.EqualTo(PlayerDamageReaction.DefeatStage.None));
            Assert.That(reaction.Darkness, Is.EqualTo(0f));
        }

        [UnityTest]
        public IEnumerator ParryTooEarly_DoesNotCatchTheBlow()
        {
            MeleeEnemy soldier = CreateSoldier(2.5f, -1);
            // Earlier than this soldier's parry window before its blow, as the owner tried in the arena.
            float pressAt = _enemySettings.Timeline.Windup - _enemySettings.ParryWindow - 0.1f;
            float timeout = Time.time + 3f;
            while (!(soldier.CurrentState == MeleeEnemy.State.Attack && soldier.StateTime >= pressAt)
                && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            _commands.ParryPressedAt = Time.time;
            while (_aliceHealth.Current == _aliceHealth.Max && soldier.CurrentState != MeleeEnemy.State.Opened
                && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(soldier.CurrentState, Is.Not.EqualTo(MeleeEnemy.State.Opened), "Too early a parry must not open the soldier.");
            Assert.That(_aliceHealth.Current, Is.LessThan(_aliceHealth.Max), "The blow lands.");
        }

        [UnityTest]
        public IEnumerator AttackInAir_IsSlash()
        {
            _motor.Teleport(new Vector2(0f, 4f));
            yield return Steps(2);
            _commands.AttackPressedAt = Time.time;
            yield return Steps(2);

            Assert.That(_motor.GetComponent<PlayerAttack>().Strike, Is.EqualTo(PlayerStrike.AirSlash));
        }

        [UnityTest]
        public IEnumerator WalkingIntoSoldier_StopsAtHisBodyAndTheTouchHurts()
        {
            MeleeEnemy soldier = CreateSoldier(4f, -1);
            float aliceHalf = _motor.GetComponent<BoxCollider2D>().bounds.extents.x;
            float farthest = soldier.transform.position.x - _enemySettings.BodySize.x * 0.5f - aliceHalf;
            HitKind firstHit = HitKind.Execution;
            void Record(HitInfo hit)
            {
                if (firstHit == HitKind.Execution)
                {
                    firstHit = hit.Kind;
                }
            }

            _aliceHealth.Damaged += Record;
            _commands.Move = Vector2.right;
            float maxX = float.NegativeInfinity;
            float until = Time.time + 2f;
            while (Time.time < until)
            {
                maxX = Mathf.Max(maxX, _motor.Position.x);
                yield return new WaitForFixedUpdate();
            }

            _aliceHealth.Damaged -= Record;
            _commands.Move = Vector2.zero;
            Assert.That(maxX, Is.LessThanOrEqualTo(farthest + 0.05f), "A living soldier is a wall on foot.");
            Assert.That(firstHit, Is.EqualTo(HitKind.Other), "Walking into him, the touch hurts before the spear lands.");
        }

        [UnityTest]
        public IEnumerator Knockback_IsSetByTheBlow()
        {
            float start = _motor.Position.x;
            _aliceHealth.ReceiveHit(new HitInfo(1, -1, null));
            yield return Steps(30);
            float byDefault = start - _motor.Position.x;

            yield return new WaitForSeconds(1f);
            start = _motor.Position.x;
            _aliceHealth.ReceiveHit(new HitInfo(1, -1, null, knockback: new Knockback(new Vector2(14f, 2f), 0.3f)));
            yield return Steps(30);
            float heavy = start - _motor.Position.x;

            Assert.That(byDefault, Is.GreaterThan(0.3f), "A hit without its own knockback still throws Alice.");
            Assert.That(heavy, Is.GreaterThan(byDefault * 1.5f), "A heavier blow throws her farther.");
        }

        [UnityTest]
        public IEnumerator Slide_PassesThroughSoldierUnhurt()
        {
            MeleeEnemy soldier = CreateSoldier(2f, -1);
            _commands.EvadePressedAt = Time.time;
            yield return Steps(Mathf.CeilToInt(TraversalMetrics.SlideDuration / Time.fixedDeltaTime) + 2);

            Assert.That(_motor.Position.x, Is.GreaterThan(soldier.transform.position.x + 1f), "The slide goes under the body.");
            Assert.That(_aliceHealth.Current, Is.EqualTo(_aliceHealth.Max), "Sliding through does not count as a touch.");
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
            public float ParryPressedAt { get; set; } = float.NegativeInfinity;
            public float InteractPressedAt { get; set; } = float.NegativeInfinity;
            public float HealPressedAt { get; set; } = float.NegativeInfinity;
            public float UseAbilityPressedAt { get; set; } = float.NegativeInfinity;
        }
    }
}
