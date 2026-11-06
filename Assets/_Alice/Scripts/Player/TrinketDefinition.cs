using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// A pocket trinket (GDD section 10): a small thing of Wonderland with one clear passive effect while it sits in
    /// one of Alice's apron pockets. Every trinket takes one pocket (owner's choice of 5 Nov 2026, GDD 0.27). The
    /// effect is data: a trinket sets only the fields it changes and leaves the rest at "no change".
    /// </summary>
    [CreateAssetMenu(menuName = "Alice/Trinket", fileName = "Trinket")]
    public sealed class TrinketDefinition : ScriptableObject
    {
        [Tooltip("Stable ID (T01, T02...): saves and caches refer to it, never to the asset name.")]
        [SerializeField] private string _id = "T00";
        [SerializeField] private Sprite _icon;

        [Header("Effect while worn")]
        [Tooltip("Damage Alice takes is multiplied by this; 1 = unchanged.")]
        [SerializeField, Range(0.1f, 1f)] private float _damageTakenMultiplier = 1f;
        [Tooltip("Seconds added to every enemy blow's parry window.")]
        [SerializeField, Min(0f)] private float _parryWindowBonus;
        [Tooltip("Clarity restored when Alice executes an enemy.")]
        [SerializeField, Min(0)] private int _executionHeal;

        public string Id => _id;
        public Sprite Icon => _icon;
        public float DamageTakenMultiplier => _damageTakenMultiplier;
        public float ParryWindowBonus => _parryWindowBonus;
        public int ExecutionHeal => _executionHeal;

        /// <summary>For the editor setup that creates the first trinkets.</summary>
        public void Configure(string id, Sprite icon, float damageTakenMultiplier, float parryWindowBonus, int executionHeal)
        {
            _id = id;
            _icon = icon;
            _damageTakenMultiplier = damageTakenMultiplier;
            _parryWindowBonus = parryWindowBonus;
            _executionHeal = executionHeal;
        }
    }
}
