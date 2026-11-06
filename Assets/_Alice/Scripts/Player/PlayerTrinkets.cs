using System.Collections.Generic;
using System.Linq;
using Alice.Core;
using Alice.Save;
using UnityEngine;

namespace Alice.Player
{
    /// <summary>
    /// Alice's pocket trinkets (GDD section 10): the ones she has found, the ones worn in her apron pockets and how
    /// many pockets she has (2 at the start, 6 at most). Every trinket takes one pocket (GDD 0.27). They are put on
    /// and taken off only at a rest; the effects of the worn ones add up.
    /// </summary>
    public sealed class PlayerTrinkets : MonoBehaviour, ITrinketReceiver, IDamageTakenModifier, ISaveParticipant
    {
        public const int StartPockets = 2;
        public const int MaxPockets = 6;

        [Tooltip("Every trinket of the game, looked up by ID.")]
        [SerializeField] private TrinketDefinition[] _catalog = new TrinketDefinition[0];
        [SerializeField] private List<string> _found = new List<string>();
        [SerializeField] private List<string> _worn = new List<string>();
        [SerializeField, Range(StartPockets, MaxPockets)] private int _pockets = StartPockets;

        public IReadOnlyList<string> Found => _found;
        public IReadOnlyList<string> Worn => _worn;
        public int Pockets => _pockets;
        public bool HasFreePocket => _worn.Count < _pockets;

        public TrinketDefinition[] Catalog
        {
            get => _catalog;
            set => _catalog = value;
        }

        public float ParryWindowBonus => WornDefinitions().Sum(trinket => trinket.ParryWindowBonus);
        public int ExecutionHeal => WornDefinitions().Sum(trinket => trinket.ExecutionHeal);

        public TrinketDefinition Definition(string trinketId)
        {
            return _catalog.FirstOrDefault(trinket => trinket != null && trinket.Id == trinketId);
        }

        public bool IsWorn(string trinketId)
        {
            return _worn.Contains(trinketId);
        }

        public bool HasTrinket(string trinketId)
        {
            return _found.Contains(trinketId);
        }

        public void ReceiveTrinket(string trinketId)
        {
            if (string.IsNullOrEmpty(trinketId) || HasTrinket(trinketId))
            {
                return;
            }

            _found.Add(trinketId);
            Autosave.Request();
        }

        /// <summary>Puts a found trinket into a free pocket or takes it out; false when every pocket is full.</summary>
        public bool Toggle(string trinketId)
        {
            if (_worn.Remove(trinketId))
            {
                return true;
            }

            if (!HasTrinket(trinketId) || !HasFreePocket)
            {
                return false;
            }

            _worn.Add(trinketId);
            return true;
        }

        public void AddPocket()
        {
            _pockets = Mathf.Min(MaxPockets, _pockets + 1);
        }

        public int ModifyDamage(int damage)
        {
            float multiplier = 1f;
            foreach (TrinketDefinition trinket in WornDefinitions())
            {
                multiplier *= trinket.DamageTakenMultiplier;
            }

            // A blow that lands always costs something, so a stack of trinkets never makes Alice untouchable.
            return damage <= 0 ? damage : Mathf.Max(1, Mathf.RoundToInt(damage * multiplier));
        }

        public void Capture(SaveData data)
        {
            data.Trinkets = new List<string>(_found);
            data.WornTrinkets = new List<string>(_worn);
            data.Pockets = _pockets;
        }

        public void Restore(SaveData data)
        {
            _pockets = Mathf.Clamp(data.Pockets <= 0 ? StartPockets : data.Pockets, StartPockets, MaxPockets);
            _found = (data.Trinkets ?? new List<string>()).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            _worn = (data.WornTrinkets ?? new List<string>()).Where(HasTrinket).Distinct().Take(_pockets).ToList();
        }

        private IEnumerable<TrinketDefinition> WornDefinitions()
        {
            foreach (string id in _worn)
            {
                TrinketDefinition trinket = Definition(id);
                if (trinket != null)
                {
                    yield return trinket;
                }
            }
        }
    }
}
