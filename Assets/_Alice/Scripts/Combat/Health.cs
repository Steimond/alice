using System;
using System.Collections.Generic;
using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// Takes hits for the body it sits on. The owner sets the numbers from its own data asset, so Alice
    /// and every enemy type keep their health with the rest of their tuning.
    /// </summary>
    public sealed class Health : MonoBehaviour, IDamageable
    {
        private readonly List<IHitImmunity> _immunities = new List<IHitImmunity>();
        private readonly List<IHitInterceptor> _interceptors = new List<IHitInterceptor>();
        private readonly List<IDamageTakenModifier> _modifiers = new List<IDamageTakenModifier>();

        private HealthPool _pool;
        private float _hitInvulnerability;
        private float _invulnerableUntil;

        public event Action<HitInfo> Damaged;
        public event Action<HitInfo> Died;

        public int Current => _pool?.Current ?? 0;
        public int Max => _pool?.Max ?? 0;
        public bool IsDead => _pool != null && _pool.IsDead;

        /// <summary>True during the short protection after a landed hit; it stops damage every frame.</summary>
        public bool IsRecovering => Time.time < _invulnerableUntil;

        private void Awake()
        {
            GetComponents(_immunities);
            GetComponents(_interceptors);
            GetComponents(_modifiers);
        }

        public void Initialize(int max, float hitInvulnerability)
        {
            _pool = new HealthPool(max);
            _hitInvulnerability = hitInvulnerability;
            _invulnerableUntil = float.NegativeInfinity;
        }

        public int Restore(int amount)
        {
            return _pool?.Restore(amount) ?? 0;
        }

        public void Refill()
        {
            _pool?.Refill();
            _invulnerableUntil = float.NegativeInfinity;
        }

        public bool ReceiveHit(HitInfo hit)
        {
            if (_pool == null || _pool.IsDead || IsRecovering || IsImmune() || IsIntercepted(hit))
            {
                return false;
            }

            int damage = hit.Damage;
            foreach (IDamageTakenModifier modifier in _modifiers)
            {
                damage = modifier.ModifyDamage(damage);
            }

            if (!_pool.Damage(damage))
            {
                return false;
            }

            _invulnerableUntil = Time.time + _hitInvulnerability;
            Damaged?.Invoke(hit);
            if (_pool.IsDead)
            {
                Died?.Invoke(hit);
            }

            return true;
        }

        /// <summary>Ends the body at once, past protection and parries, e.g. for an execution.</summary>
        public void Kill(HitInfo hit)
        {
            if (_pool == null || _pool.IsDead)
            {
                return;
            }

            _pool.Damage(_pool.Current);
            Died?.Invoke(hit);
        }

        private bool IsIntercepted(HitInfo hit)
        {
            foreach (IHitInterceptor interceptor in _interceptors)
            {
                if (interceptor.TryIntercept(hit))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsImmune()
        {
            foreach (IHitImmunity immunity in _immunities)
            {
                if (immunity.IsImmune)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
