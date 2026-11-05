using System;
using System.Collections.Generic;
using Alice.Core;
using Alice.Save;
using UnityEngine;

namespace Alice.Abilities
{
    /// <summary>
    /// Permanent abilities Alice has obtained, addressed by ID. A new one is an autosave point, so a later
    /// defeat cannot take it back (GDD section 10).
    /// </summary>
    public sealed class AbilityInventory : MonoBehaviour, ISaveParticipant
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
            Autosave.Request();
        }

        public void Capture(SaveData data)
        {
            data.Abilities = new List<string>(_unlocked);
        }

        public void Restore(SaveData data)
        {
            foreach (string id in data.Abilities)
            {
                if (!string.IsNullOrEmpty(id) && !Has(id))
                {
                    _unlocked.Add(id);
                    Unlocked?.Invoke(id);
                }
            }
        }
    }
}
