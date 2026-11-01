using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alice.Abilities
{
    /// <summary>Permanent abilities Alice has obtained, addressed by ID.</summary>
    public sealed class AbilityInventory : MonoBehaviour
    {
        [SerializeField] private List<string> _unlocked = new List<string>();

        public event Action<string> Unlocked;

        public bool Has(string id)
        {
            return _unlocked.Contains(id);
        }

        public void Unlock(string id)
        {
            if (Has(id))
            {
                return;
            }

            _unlocked.Add(id);
            Unlocked?.Invoke(id);
        }
    }
}
