using System.Collections.Generic;
using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// Box query for a weapon swing. Hitboxes are numbers in data, not colliders on art, so reach can be
    /// tuned without redrawing frames (GDD section 4).
    /// </summary>
    public static class MeleeHitbox
    {
        /// <summary>
        /// Hits every damageable body in the box that this swing has not hit yet.
        /// <paramref name="struck"/> belongs to one swing; clear it when a new swing starts.
        /// </summary>
        public static int Strike(Vector2 center, Vector2 size, HitInfo hit, GameObject attacker,
            HashSet<IDamageable> struck, Collider2D[] buffer)
        {
            // Enemy bodies are triggers so Alice can slide through them; the swing must still find them.
            ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
            int count = Physics2D.OverlapBox(center, size, 0f, filter, buffer);
            int landed = 0;
            for (int index = 0; index < count; index++)
            {
                Collider2D other = buffer[index];
                GameObject owner = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
                if (owner == attacker || !owner.TryGetComponent(out IDamageable target) || !struck.Add(target))
                {
                    continue;
                }

                if (target.ReceiveHit(hit))
                {
                    landed++;
                }
            }

            return landed;
        }
    }
}
