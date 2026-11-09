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
    /// <summary>The biome 1 archetypes (GDD section 9): moth, race rat, duck washerwoman and laundry sheet.</summary>
    public sealed class EnemyArchetypeTests
    {
        private readonly List<Object> _created = new List<Object>();
        private ScriptedCommands _commands;
        private PlayerMotor _motor;
        private Health _aliceHealth;
        private PlayerRestraint _restraint;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameObject floor = Track(new GameObject("Floor"));
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(100f, 1f);

            PlayerMovementSettings movement = Track(ScriptableObject.CreateInstance<PlayerMovementSettings>());
            PlayerCombatSettings combat = Track(ScriptableObject.CreateInstance<PlayerCombatSettings>());

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
            _restraint = alice.AddComponent<PlayerRestraint>();
            alice.SetActive(true);

            yield return Steps(10);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                if (created != null)
                {
                    Object.Destroy(created);
                }
            }

            _created.Clear();
        }

        [UnityTest]
        public IEnumerator Moth_DivesThroughAlice_ThenHangsExposedAboveGroundBlows()
        {
            FlyingDiverSettings settings = Track(ScriptableObject.CreateInstance<FlyingDiverSettings>());
            FlyingDiver moth = Create<FlyingDiver>(new Vector2(3f, 7f), enemy => enemy.Settings = settings);

            bool exposed = false;
            float exposedBottom = 0f;
            float timeout = Time.time + 6f;
            while ((_aliceHealth.Current == _aliceHealth.Max || !exposed) && Time.time < timeout)
            {
                if (moth.CurrentState == FlyingDiver.State.Exposed && moth.StateTime > settings.ExposedRiseTime + 0.05f)
                {
                    exposed = true;
                    exposedBottom = moth.Position.y;
                }

                yield return new WaitForFixedUpdate();
            }

            Assert.That(_aliceHealth.Current, Is.LessThan(_aliceHealth.Max), "The dive sweeps through Alice.");
            Assert.That(exposed, Is.True, "After the dive it hangs exposed.");
            // Ground blows reach 2.8 tiles (slash 1.5 ± 1.3); a jump with a blow reaches it.
            Assert.That(exposedBottom, Is.GreaterThan(2.8f));
            Assert.That(exposedBottom, Is.LessThan(TraversalMetrics.JumpHeight + 2.5f));
        }

        [UnityTest]
        public IEnumerator Moth_BlowInTheAir_DoesNotBringItDown()
        {
            FlyingDiverSettings settings = Track(ScriptableObject.CreateInstance<FlyingDiverSettings>());
            _motor.Teleport(new Vector2(-20f, 0.02f));
            FlyingDiver moth = Create<FlyingDiver>(new Vector2(3f, 7f), enemy => enemy.Settings = settings);
            Health health = moth.GetComponent<Health>();
            yield return Steps(5);

            health.ReceiveHit(new HitInfo(1, 1, _motor.gameObject, HitKind.Slash));
            yield return Steps(40);
            Assert.That(health.Current, Is.EqualTo(health.Max - 1), "The blow lands.");
            Assert.That(moth.Position.y, Is.GreaterThan(5f), "It keeps flying: only the stun drops it.");
            Assert.That(moth.CurrentState, Is.Not.EqualTo(FlyingDiver.State.Falling));
        }

        [UnityTest]
        public IEnumerator Unseen_ItStrollsAboutItsPost_AndStandsNowAndThen()
        {
            ThrowerSettings settings = Track(ScriptableObject.CreateInstance<ThrowerSettings>());
            _motor.Teleport(new Vector2(-40f, 0.02f));
            Thrower duck = Create<Thrower>(new Vector2(0f, 0f), enemy => enemy.Settings = settings);

            bool walked = false;
            bool stood = false;
            float farthest = 0f;
            float until = Time.time + settings.PatrolPause.y + 2f * settings.PatrolRange / settings.PatrolSpeed + 2f;
            while (Time.time < until && !(walked && stood && farthest > 0.5f))
            {
                walked |= duck.CurrentState == Thrower.State.Walk;
                stood |= duck.CurrentState == Thrower.State.Guard && duck.StateTime > 0.5f;
                farthest = Mathf.Max(farthest, Mathf.Abs(duck.Position.x));
                Assert.That(Mathf.Abs(duck.Position.x), Is.LessThanOrEqualTo(settings.PatrolRange + 0.05f), "It keeps near its post.");
                yield return new WaitForFixedUpdate();
            }

            Assert.That(walked, Is.True, "It walks about.");
            Assert.That(stood, Is.True, "It stands now and then.");
            Assert.That(farthest, Is.GreaterThan(0.5f));
        }

        [UnityTest]
        public IEnumerator Stun_IsAChance_RolledOncePerLife()
        {
            ThrowerSettings settings = Track(ScriptableObject.CreateInstance<ThrowerSettings>());
            SetExecution(settings, "duck_throat");
            SetStunChance(settings, 0f);
            // Three health, the threshold at two: one blow crosses it, the next still leaves the duck alive.
            typeof(EnemySettings).GetField("_stunAtHealth",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(settings, 2);
            _motor.Teleport(new Vector2(-30f, 0.02f));
            Thrower duck = Create<Thrower>(new Vector2(0f, 0f), enemy => enemy.Settings = settings);
            Health health = duck.GetComponent<Health>();
            yield return Steps(5);

            health.ReceiveHit(new HitInfo(health.Current - settings.StunAtHealth, 1, _motor.gameObject, HitKind.Thrust));
            yield return Steps(2);
            Assert.That(duck.IsStunned, Is.False, "The roll failed: no execution this time.");

            SetStunChance(settings, 1f);
            health.ReceiveHit(new HitInfo(1, 1, _motor.gameObject, HitKind.Thrust));
            Assert.That(health.IsDead, Is.False);
            yield return Steps(2);
            Assert.That(duck.IsStunned, Is.False, "One roll per life: later blows do not roll again.");

            WorldReset.Raise();
            yield return Steps(2);
            health.ReceiveHit(new HitInfo(health.Current - settings.StunAtHealth, 1, _motor.gameObject, HitKind.Thrust));
            yield return Steps(2);
            Assert.That(duck.IsStunned, Is.True, "A new life rolls anew.");
        }

        [UnityTest]
        public IEnumerator Racer_ChargesThroughAlice_IntoTheWall_AndReels()
        {
            GameObject wall = Track(new GameObject("Wall"));
            wall.transform.position = new Vector3(-7f, 3f, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(1f, 6f);

            ChargerSettings settings = Track(ScriptableObject.CreateInstance<ChargerSettings>());
            Charger racer = Create<Charger>(new Vector2(4f, 0f), enemy =>
            {
                enemy.Settings = settings;
                enemy.InitialFacing = -1;
            });

            bool dazed = false;
            float timeout = Time.time + 5f;
            while (!dazed && Time.time < timeout)
            {
                dazed = racer.CurrentState == Charger.State.Dazed;
                yield return new WaitForFixedUpdate();
            }

            Assert.That(_aliceHealth.Current, Is.EqualTo(_aliceHealth.Max - settings.ChargeDamage),
                "The ram hits Alice once on its way.");
            Assert.That(dazed, Is.True, "Head first into the wall it reels.");
        }

        [UnityTest]
        public IEnumerator Racer_ChargingThroughAnotherEnemy_DoesNotHurtIt()
        {
            ChargerSettings settings = Track(ScriptableObject.CreateInstance<ChargerSettings>());
            Charger racer = Create<Charger>(new Vector2(6f, 0f), enemy =>
            {
                enemy.Settings = settings;
                enemy.InitialFacing = -1;
            });
            Thrower duck = Create<Thrower>(new Vector2(2.5f, 0f),
                enemy => enemy.Settings = Track(ScriptableObject.CreateInstance<ThrowerSettings>()));
            Health duckHealth = duck.GetComponent<Health>();

            float timeout = Time.time + 4f;
            while (_aliceHealth.Current == _aliceHealth.Max && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(_aliceHealth.Current, Is.LessThan(_aliceHealth.Max), "The ram reaches Alice.");
            Assert.That(duckHealth.Current, Is.EqualTo(duckHealth.Max), "Enemies never hurt each other.");
        }

        [UnityTest]
        public IEnumerator Racer_Ram_CannotBeParried()
        {
            ChargerSettings settings = Track(ScriptableObject.CreateInstance<ChargerSettings>());
            Assert.That(settings.ChargeParryWindow, Is.EqualTo(0f));
            yield break;
        }

        [UnityTest]
        public IEnumerator Duck_ThrowsAClothespinOnAnArc_ThatHitsAlice()
        {
            ArcProjectile pin = CreateProjectileTemplate();
            ThrowerSettings settings = Track(ScriptableObject.CreateInstance<ThrowerSettings>());
            settings.Projectile = pin;
            Thrower duck = Create<Thrower>(new Vector2(7f, 0f), enemy => enemy.Settings = settings);

            bool threw = false;
            float timeout = Time.time + 4f;
            while (_aliceHealth.Current == _aliceHealth.Max && Time.time < timeout)
            {
                threw |= duck.CurrentState == Thrower.State.Throw;
                yield return new WaitForFixedUpdate();
            }

            Assert.That(threw, Is.True);
            Assert.That(_aliceHealth.Current, Is.LessThan(_aliceHealth.Max), "The clothespin lands on Alice.");
        }

        [UnityTest]
        public IEnumerator Sheet_WrapsAlice_SqueezesHer_AndHerStruggleTearsHerFree()
        {
            AmbusherSettings settings = Track(ScriptableObject.CreateInstance<AmbusherSettings>());
            Ambusher sheet = Create<Ambusher>(new Vector2(0.5f, 0f), enemy => enemy.Settings = settings);

            float timeout = Time.time + 3f;
            while (!_restraint.IsRestrained && Time.time < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(_restraint.IsRestrained, Is.True, "Standing under it, Alice is wrapped.");
            yield return new WaitForSeconds(settings.Drop + 0.1f);
            Assert.That(sheet.CurrentState, Is.EqualTo(Ambusher.State.Hold));
            Vector2 heldAt = _motor.Position;

            yield return new WaitForSeconds(settings.SqueezeInterval + 0.1f);
            Assert.That(_aliceHealth.Current, Is.LessThan(_aliceHealth.Max), "The sheet squeezes.");
            Assert.That(Vector2.Distance(_motor.Position, heldAt), Is.LessThan(0.05f), "She stays inside.");

            for (int press = 0; press < settings.PressesToEscape; press++)
            {
                _commands.AttackPressedAt = Time.time;
                yield return Steps(3);
            }

            Assert.That(_restraint.IsRestrained, Is.False, "Enough struggling tears her free.");
            Assert.That(_restraint.IsTearingFree, Is.True, "She rips the cloth apart where she stands.");
            yield return Steps(2);
            Assert.That(sheet.IsDead, Is.True, "Torn apart, the sheet is dead.");
            Assert.That(sheet.ClipId, Is.EqualTo("torn"));
        }

        [UnityTest]
        public IEnumerator Sheet_CutWhileItHangs_DiesAtOneBlow_WithoutWrapping()
        {
            AmbusherSettings settings = Track(ScriptableObject.CreateInstance<AmbusherSettings>());
            SetMaxHealth(settings, 1);
            _motor.Teleport(new Vector2(-2.6f, 0.02f));
            Ambusher sheet = Create<Ambusher>(new Vector2(0f, 0f), enemy => enemy.Settings = settings);
            yield return Steps(5);
            Assert.That(sheet.CurrentState, Is.EqualTo(Ambusher.State.Hanging), "A thrust's length away it stays still.");

            sheet.GetComponent<Health>().ReceiveHit(new HitInfo(1, 1, _motor.gameObject, HitKind.Thrust));
            yield return Steps(2);
            Assert.That(sheet.IsDead, Is.True, "One blow cuts it.");
            Assert.That(sheet.ClipId, Is.EqualTo("cut"), "Its lower half falls, the upper half stays on the line.");
            yield return new WaitForSeconds(settings.Drop + 0.2f);
            Assert.That(_restraint.IsRestrained, Is.False);
        }

        [UnityTest]
        public IEnumerator Rest_BringsEveryArchetypeBackAliveAtItsPost()
        {
            _motor.Teleport(new Vector2(-30f, 0.02f));
            FlyingDiver moth = Create<FlyingDiver>(new Vector2(10f, 7f),
                enemy => enemy.Settings = Track(ScriptableObject.CreateInstance<FlyingDiverSettings>()));
            Charger racer = Create<Charger>(new Vector2(14f, 0f),
                enemy => enemy.Settings = Track(ScriptableObject.CreateInstance<ChargerSettings>()));
            Thrower duck = Create<Thrower>(new Vector2(18f, 0f),
                enemy => enemy.Settings = Track(ScriptableObject.CreateInstance<ThrowerSettings>()));
            Ambusher sheet = Create<Ambusher>(new Vector2(22f, 0f),
                enemy => enemy.Settings = Track(ScriptableObject.CreateInstance<AmbusherSettings>()));
            yield return Steps(5);

            var enemies = new EnemyBase[] { moth, racer, duck, sheet };
            foreach (EnemyBase enemy in enemies)
            {
                enemy.GetComponent<Health>().Kill(new HitInfo(1, 1, _motor.gameObject, HitKind.Thrust));
            }

            yield return Steps(60);
            WorldReset.Raise();
            yield return Steps(2);

            foreach (EnemyBase enemy in enemies)
            {
                Assert.That(enemy.IsDead, Is.False, enemy.name);
                Assert.That(enemy.GetComponent<Health>().Current, Is.EqualTo(enemy.BaseSettings.MaxHealth), enemy.name);
            }

            Assert.That(moth.CurrentState, Is.EqualTo(FlyingDiver.State.Hover));
            Assert.That(sheet.CurrentState, Is.EqualTo(Ambusher.State.Hanging));
        }

        [UnityTest]
        public IEnumerator Duck_NearlyBeaten_IsStunnedOnce_AndItsExecutionRunsTheExecutionDeath()
        {
            ThrowerSettings settings = Track(ScriptableObject.CreateInstance<ThrowerSettings>());
            SetExecution(settings, "duck_throat");
            _motor.Teleport(new Vector2(-30f, 0.02f));
            Thrower duck = Create<Thrower>(new Vector2(0f, 0f), enemy => enemy.Settings = settings);
            Health health = duck.GetComponent<Health>();
            yield return Steps(5);

            health.ReceiveHit(new HitInfo(health.Current - settings.StunAtHealth, 1, _motor.gameObject, HitKind.Thrust));
            yield return Steps(2);
            Assert.That(duck.IsStunned, Is.True);
            Assert.That(duck.CanBeExecuted, Is.True);
            Assert.That(duck.ClipId, Is.EqualTo("stunned"));

            duck.BeginExecution(_motor.gameObject, 1);
            yield return Steps(1);
            Assert.That(duck.IsDead, Is.True);
            Assert.That(duck.KillingBlow, Is.EqualTo(HitKind.Execution));
            Assert.That(duck.ClipId, Is.EqualTo("death"));
            Assert.That(duck.ClipSpan, Is.EqualTo(settings.ExecutionTime), "The death is spread over Alice's execution.");
        }

        [UnityTest]
        public IEnumerator Moth_StunnedByTheBlowThatDropsIt_FallsAndReelsOnTheFloor_ThenLiftsOff()
        {
            FlyingDiverSettings settings = Track(ScriptableObject.CreateInstance<FlyingDiverSettings>());
            SetExecution(settings, "moth_pin");
            _motor.Teleport(new Vector2(-30f, 0.02f));
            FlyingDiver moth = Create<FlyingDiver>(new Vector2(3f, 7f), enemy => enemy.Settings = settings);
            Health health = moth.GetComponent<Health>();
            yield return Steps(5);

            health.ReceiveHit(new HitInfo(health.Current - settings.StunAtHealth, 1, _motor.gameObject, HitKind.Slash));
            yield return Steps(2);
            Assert.That(moth.IsStunned, Is.True, "The near-fatal blow stuns it as it drops.");
            Assert.That(moth.ClipId, Is.EqualTo("fall"), "Still in the air it shows its fall.");

            yield return Steps(40);
            Assert.That(moth.Position.y, Is.EqualTo(0f).Within(0.05f), "Stunned, it still falls to the floor.");
            Assert.That(moth.ClipId, Is.EqualTo("stunned"));

            yield return new WaitForSeconds(settings.StunnedTime);
            Assert.That(moth.IsStunned, Is.False);
            Assert.That(moth.CurrentState == FlyingDiver.State.Rising || moth.CurrentState == FlyingDiver.State.Climb,
                Is.True, "When the stun runs out it gets up and lifts off.");
        }

        [UnityTest]
        public IEnumerator Sheet_IsNeverStunned()
        {
            AmbusherSettings settings = Track(ScriptableObject.CreateInstance<AmbusherSettings>());
            SetExecution(settings, "sheet_rip");
            _motor.Teleport(new Vector2(-3f, 0.02f));
            Ambusher sheet = Create<Ambusher>(new Vector2(0f, 0f), enemy => enemy.Settings = settings);
            Health health = sheet.GetComponent<Health>();
            yield return Steps(5);

            health.ReceiveHit(new HitInfo(health.Current - settings.StunAtHealth, 1, _motor.gameObject, HitKind.Thrust));
            yield return Steps(5);
            Assert.That(sheet.CurrentState, Is.EqualTo(Ambusher.State.Hanging), "A sturdier sheet just takes the blow.");
            Assert.That(sheet.IsStunned, Is.False, "Cloth does not reel.");
            Assert.That(sheet.CanBeExecuted, Is.False);
        }

        [UnityTest]
        public IEnumerator Sheet_TwitchesWhileAliceIsNear_AndCatchesHerForCertainWhenSheStepsUnder()
        {
            AmbusherSettings settings = Track(ScriptableObject.CreateInstance<AmbusherSettings>());
            _motor.Teleport(new Vector2(-1.8f, 0.02f));
            Ambusher sheet = Create<Ambusher>(new Vector2(0f, 0f), enemy => enemy.Settings = settings);
            yield return Steps(5);
            Assert.That(sheet.CurrentState, Is.EqualTo(Ambusher.State.Tremble), "Near it, she sees it twitch.");

            yield return new WaitForSeconds(settings.Tremble * 2.5f);
            Assert.That(sheet.CurrentState, Is.EqualTo(Ambusher.State.Tremble), "It keeps twitching, it does not drop.");
            Assert.That(_restraint.IsRestrained, Is.False);

            _motor.Teleport(new Vector2(4f, 0.02f));
            yield return Steps(3);
            Assert.That(sheet.CurrentState, Is.EqualTo(Ambusher.State.Hanging), "She went away: it calms down.");

            // Running across: the frame she is under it, she is held, however fast she goes.
            _motor.Teleport(new Vector2(0.6f, 0.02f));
            yield return Steps(2);
            Assert.That(_restraint.IsRestrained, Is.True, "Stepping under it, she is caught.");
            Vector2 heldAt = _motor.Position;
            yield return new WaitForSeconds(settings.Drop + 0.1f);
            Assert.That(sheet.CurrentState, Is.EqualTo(Ambusher.State.Hold));
            Assert.That(Vector2.Distance(_motor.Position, heldAt), Is.LessThan(0.05f), "The fall does not let her go.");
        }

        private static void SetMaxHealth(EnemySettings settings, int value)
        {
            typeof(EnemySettings).GetField("_maxHealth",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(settings, value);
        }

        private static void SetStunChance(EnemySettings settings, float chance)
        {
            typeof(EnemySettings).GetField("_stunChance",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(settings, chance);
        }

        // The execution ID is authored in the asset; tests have no editor to set it.
        private static void SetExecution(EnemySettings settings, string id)
        {
            typeof(EnemySettings).GetField("_executionId",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(settings, id);
            // Stun tests need the stun every time; the chance itself is tested on its own.
            SetStunChance(settings, 1f);
        }

        private T Create<T>(Vector2 position, System.Action<T> configure) where T : EnemyBase
        {
            GameObject root = Track(new GameObject(typeof(T).Name));
            root.SetActive(false);
            root.transform.position = position;
            root.AddComponent<Rigidbody2D>();
            root.AddComponent<BoxCollider2D>();
            root.AddComponent<Health>();
            T enemy = root.AddComponent<T>();
            configure(enemy);
            root.SetActive(true);
            return enemy;
        }

        private ArcProjectile CreateProjectileTemplate()
        {
            GameObject template = Track(new GameObject("PinTemplate"));
            template.SetActive(false);
            ArcProjectile projectile = template.AddComponent<ArcProjectile>();
            return projectile;
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
