using System;
using Alice.Core;
using UnityEngine;

namespace Alice.Combat
{
    /// <summary>
    /// Frames of one enemy type as named clips. The archetype names the clip it wants; the type only has to supply
    /// clips under those names. Art faces right and stands on the bottom-centre pivot.
    /// </summary>
    [CreateAssetMenu(menuName = "Alice/Enemy Clip Set", fileName = "EnemyClips")]
    public sealed class EnemyClipSet : ScriptableObject
    {
        [Serializable]
        public struct Clip
        {
            [SerializeField] private string _id;
            [SerializeField] private Sprite[] _frames;
            [SerializeField, Min(0.01f)] private float _frameTime;
            [Tooltip("Loops; otherwise it holds the last frame.")]
            [SerializeField] private bool _loop;

            public Clip(string id, Sprite[] frames, float frameTime, bool loop)
            {
                _id = id;
                _frames = frames;
                _frameTime = frameTime;
                _loop = loop;
            }

            public string Id => _id;
            public Sprite[] Frames => _frames;
            public float FrameTime => _frameTime;
            public bool Loop => _loop;
        }

        [SerializeField] private Clip[] _clips = new Clip[0];

        public Clip[] Clips => _clips;

        public bool TryGet(string id, out Clip clip)
        {
            foreach (Clip candidate in _clips)
            {
                if (candidate.Id == id && candidate.Frames != null && candidate.Frames.Length > 0)
                {
                    clip = candidate;
                    return true;
                }
            }

            clip = default;
            return false;
        }

        /// <summary>
        /// The death drawn for this killing blow ("death_thrust", "death_spin"…), else the plain clip.
        /// </summary>
        public bool TryGetFor(string id, HitKind kind, out Clip clip)
        {
            return TryGet(id + "_" + kind.ToString().ToLowerInvariant(), out clip) || TryGet(id, out clip);
        }

        public void Assign(Clip[] clips)
        {
            _clips = clips;
        }

        /// <summary>The frame at <paramref name="time"/>, or spread over <paramref name="span"/> when it is set.</summary>
        public static Sprite FrameAt(Clip clip, float time, float span)
        {
            Sprite[] frames = clip.Frames;
            int frame = span > 0f
                ? Mathf.FloorToInt(Mathf.Clamp01(time / span) * frames.Length)
                : Mathf.FloorToInt(Mathf.Max(0f, time) / clip.FrameTime);
            frame = clip.Loop && span <= 0f ? frame % frames.Length : Mathf.Min(frame, frames.Length - 1);
            return frames[Mathf.Max(0, frame)];
        }
    }
}
