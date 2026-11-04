using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// What Alice carries into a fight (GDD section 4): the special technique chosen at a rest. It cannot be
    /// switched in combat, so there is no "next technique" button. Rests and saves set it in M3.
    /// </summary>
    public sealed class PlayerLoadout : MonoBehaviour
    {
        [Tooltip("Technique ID from TechniqueIds; empty = none chosen.")]
        [SerializeField] private string _equippedTechnique = TechniqueIds.SpinStrike;

        public string EquippedTechnique => _equippedTechnique;

        /// <summary>Called at a rest.</summary>
        public void Equip(string techniqueId)
        {
            _equippedTechnique = techniqueId;
        }
    }
}
