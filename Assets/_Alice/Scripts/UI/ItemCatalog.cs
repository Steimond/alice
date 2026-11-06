using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Alice.UI
{
    /// <summary>
    /// How found items are shown (GDD 0.28): for each stable item ID (A01, T01...) its icon and its name from the
    /// Items localization table. Abilities, trinkets and everything found later go through the same list.
    /// </summary>
    [CreateAssetMenu(menuName = "Alice/Item Catalog", fileName = "ItemCatalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string Id;
            public Sprite Icon;
            public LocalizedString Name = new LocalizedString();
        }

        [SerializeField] private Entry[] _entries = new Entry[0];

        public Entry[] Entries
        {
            get => _entries;
            set => _entries = value;
        }

        public Entry Find(string itemId)
        {
            return Array.Find(_entries, entry => entry != null && entry.Id == itemId);
        }
    }
}
