using System.Collections.Generic;
using Alice.Save;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// What Alice carries into a fight (GDD section 4): the special technique chosen at a rest from the ones
    /// she knows. It cannot be switched in combat, so there is no "next technique" button.
    /// </summary>
    public sealed class PlayerLoadout : MonoBehaviour, ISaveParticipant
    {
        [Tooltip("Technique IDs from TechniqueIds that Alice knows; bought from the master later in the game.")]
        [SerializeField] private List<string> _knownTechniques = new List<string> { TechniqueIds.SpinStrike };
        [Tooltip("Technique ID from TechniqueIds; empty = none chosen.")]
        [SerializeField] private string _equippedTechnique = TechniqueIds.SpinStrike;

        public string EquippedTechnique => _equippedTechnique;
        public IReadOnlyList<string> KnownTechniques => _knownTechniques;

        /// <summary>Called at a rest; empty takes the technique off, an unknown one is ignored.</summary>
        public void Equip(string techniqueId)
        {
            if (string.IsNullOrEmpty(techniqueId) || _knownTechniques.Contains(techniqueId))
            {
                _equippedTechnique = techniqueId ?? string.Empty;
            }
        }

        public void Learn(string techniqueId)
        {
            if (!string.IsNullOrEmpty(techniqueId) && !_knownTechniques.Contains(techniqueId))
            {
                _knownTechniques.Add(techniqueId);
            }
        }

        public void Capture(SaveData data)
        {
            data.EquippedTechnique = _equippedTechnique;
            data.KnownTechniques = new List<string>(_knownTechniques);
        }

        public void Restore(SaveData data)
        {
            foreach (string technique in data.KnownTechniques)
            {
                Learn(technique);
            }

            Equip(data.EquippedTechnique);
        }
    }
}
