using Alice.Player;
using NUnit.Framework;
using UnityEngine;

namespace Alice.Tests
{
    public sealed class PlayerSpriteSetTests
    {
        [Test]
        public void IdleAt_LoopsTheBreathAndFallsBackToTheStance()
        {
            PlayerSpriteSet set = ScriptableObject.CreateInstance<PlayerSpriteSet>();
            Texture2D texture = new Texture2D(4, 4);
            Sprite stance = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.zero);
            Sprite[] breath =
            {
                stance,
                Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero),
                Sprite.Create(texture, new Rect(0, 0, 3, 3), Vector2.zero)
            };

            try
            {
                set.Assign(stance, new Sprite[0]);
                Assert.That(set.IdleAt(5f), Is.SameAs(stance), "Without breath frames she stands still.");

                set.AssignIdleBreath(breath);
                Assert.That(set.IdleAt(0f), Is.SameAs(stance), "A stop starts at the stance.");
                Assert.That(set.IdleAt(0.31f), Is.SameAs(breath[1]));
                Assert.That(set.IdleAt(0.91f), Is.SameAs(stance), "The breath loops.");

                // Delay 6 s with a 0.9 s breath rounds up to 7 breaths (6.3 s); the fidget then plays once,
                // two frames of 0.15 s.
                Sprite[] fidget = { breath[2], breath[1] };
                set.AssignIdleFidget(fidget);
                Assert.That(set.IdleAt(6.2f), Is.SameAs(breath[2]), "Still breathing before the whole breath ends.");
                Assert.That(set.IdleAt(6.35f), Is.SameAs(fidget[0]));
                Assert.That(set.IdleAt(6.47f), Is.SameAs(fidget[1]));
                Assert.That(set.IdleAt(6.65f), Is.SameAs(stance), "After the fidget the breath starts over.");
            }
            finally
            {
                foreach (Sprite sprite in breath)
                {
                    Object.DestroyImmediate(sprite);
                }

                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(set);
            }
        }
    }
}
