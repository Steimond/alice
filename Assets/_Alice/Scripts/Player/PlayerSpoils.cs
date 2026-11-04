using Alice.Core;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// What Alice takes from the enemies she beats: Queen's decrees, the currency, and confidence for her
    /// special techniques (GDD sections 4 and 10). An execution gives more confidence than a plain kill.
    /// Losing the decrees at the place of defeat arrives with rests in M3.
    /// </summary>
    public sealed class PlayerSpoils : MonoBehaviour
    {
        [SerializeField] private PlayerCombatSettings _settings;

        public PlayerCombatSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        public int Decrees { get; private set; }
        public ConfidencePool Confidence { get; private set; }

        private void Awake()
        {
            if (_settings == null)
            {
                Debug.LogError("PlayerSpoils needs combat settings.", this);
                enabled = false;
                return;
            }

            Confidence = new ConfidencePool(_settings.ConfidenceMax);
        }

        private void OnEnable()
        {
            EnemyDefeats.Happened += OnEnemyDefeated;
        }

        private void OnDisable()
        {
            EnemyDefeats.Happened -= OnEnemyDefeated;
        }

        private void OnEnemyDefeated(EnemyDefeat defeat)
        {
            if (defeat.KillingBlow.Source != gameObject)
            {
                return;
            }

            Decrees += defeat.Decrees;
            float factor = defeat.KillingBlow.Kind == HitKind.Execution ? _settings.ExecutionConfidenceFactor : 1f;
            Confidence.Gain(defeat.Confidence * factor);
        }
    }
}
