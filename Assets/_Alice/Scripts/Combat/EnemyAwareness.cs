using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// Two zones instead of one: an enemy notices Alice inside its sight box, and once it has, keeps her until she
    /// leaves a wider box. Both boxes are around the enemy where it stands now, not around its post, so a chase does
    /// not end the moment Alice steps a little past where it started (owner's remark, 9 November 2026).
    /// </summary>
    public sealed class EnemyAwareness
    {
        public bool Engaged { get; private set; }

        /// <summary>Updates and returns whether Alice is followed, by her offset from the enemy.</summary>
        public bool Track(Vector2 offset, float sightRange, float sightHeight, float loseRange, float loseHeight)
        {
            Engaged = Engaged
                ? Mathf.Abs(offset.x) <= Mathf.Max(loseRange, sightRange) && Mathf.Abs(offset.y) <= Mathf.Max(loseHeight, sightHeight)
                : Mathf.Abs(offset.x) <= sightRange && Mathf.Abs(offset.y) <= sightHeight;
            return Engaged;
        }

        public void Forget()
        {
            Engaged = false;
        }
    }
}
