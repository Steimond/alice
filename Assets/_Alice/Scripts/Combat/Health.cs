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
        }

        public void Initialize(int max, float hitInvulnerability)
        {
            _pool = new HealthPool(max);
            _hitInvulnerability = hitInvulnerability;
            _invulnerableUntil = float.NegativeInfinity;
        }

        public void Refill()
        {
            _pool?.Refill();
            _invulnerableUntil = float.NegativeInfinity;
        }

        public bool ReceiveHit(HitInfo hit)
        {
            if (_pool == null || _pool.IsDead || IsRecovering || IsImmune())
            {
                return false;
            }

            if (!_pool.Damage(hit.Damage))
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
