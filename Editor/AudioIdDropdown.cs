using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yasin.AudioSystem.Editor
{
    public readonly struct AudioEntryOption
    {
        public readonly string Guid;
        public readonly string Id;
        public readonly string CatalogName;
        public readonly UnityEngine.Object Asset;

        public AudioEntryOption(string guid, string id, string catalogName, UnityEngine.Object asset)
        {
            Guid = guid;
            Id = id;
            CatalogName = catalogName;
            Asset = asset;
        }
    }

    /// <summary>
    /// Searchable, prefix-filtered, grouped dropdown menu for AudioKey and [AudioId(filterPrefix)] fields.
    /// </summary>
    public sealed class AudioIdDropdown : AdvancedDropdown
    {
        private sealed class AudioDropdownItem : AdvancedDropdownItem
        {
            public string Guid { get; }
            public string SoundId { get; }
            public bool IsClearOption { get; }

            public AudioDropdownItem(string name, string guid, string soundId, bool isClearOption = false) : base(name)
            {
                Guid = guid;
                SoundId = soundId;
                IsClearOption = isClearOption;
            }
        }

        private readonly IReadOnlyList<AudioEntryOption> _entries;
        private readonly string _filterPrefix;
        private readonly Action<string, string> _onSelected;

        public AudioIdDropdown(
            AdvancedDropdownState state,
            IReadOnlyList<AudioEntryOption> entries,
            string filterPrefix,
            Action<string, string> onSelected) : base(state)
        {
            _entries = entries;
            _filterPrefix = filterPrefix;
            _onSelected = onSelected;
            minimumSize = new Vector2(300f, 320f);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            string title = string.IsNullOrEmpty(_filterPrefix)
                ? "Audio Catalog"
                : $"Audio Catalog (Filter: {_filterPrefix}*)";
            AdvancedDropdownItem root = new AdvancedDropdownItem(title);

            root.AddChild(new AudioDropdownItem("None (Clear)", string.Empty, string.Empty, isClearOption: true));

            if (_entries == null || _entries.Count == 0)
            {
                root.AddChild(new AudioDropdownItem("(No sounds found in any AudioCatalog)", string.Empty, string.Empty));
                return root;
            }

            Dictionary<string, AdvancedDropdownItem> groupDict = new Dictionary<string, AdvancedDropdownItem>(StringComparer.OrdinalIgnoreCase);
            int matchedCount = 0;

            for (int i = 0; i < _entries.Count; i++)
            {
                AudioEntryOption entry = _entries[i];
                if (string.IsNullOrWhiteSpace(entry.Id))
                    continue;

                if (!string.IsNullOrEmpty(_filterPrefix) &&
                    !entry.Id.StartsWith(_filterPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                matchedCount++;
                string label = string.IsNullOrEmpty(entry.CatalogName)
                    ? entry.Id
                    : $"{entry.Id}  [{entry.CatalogName}]";

                int firstUnder = entry.Id.IndexOf('_');
                if (firstUnder > 0)
                {
                    string prefix = entry.Id.Substring(0, firstUnder);
                    if (!groupDict.TryGetValue(prefix, out AdvancedDropdownItem groupItem))
                    {
                        groupItem = new AdvancedDropdownItem(prefix.ToUpperInvariant());
                        groupDict[prefix] = groupItem;
                        root.AddChild(groupItem);
                    }
                    groupItem.AddChild(new AudioDropdownItem(label, entry.Guid, entry.Id));
                }
                else
                {
                    root.AddChild(new AudioDropdownItem(label, entry.Guid, entry.Id));
                }
            }

            if (matchedCount == 0 && !string.IsNullOrEmpty(_filterPrefix))
            {
                root.AddChild(new AudioDropdownItem($"(No sounds matching '{_filterPrefix}*')", string.Empty, string.Empty));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);
            if (item is AudioDropdownItem audioItem && (audioItem.IsClearOption || !string.IsNullOrEmpty(audioItem.SoundId) || !string.IsNullOrEmpty(audioItem.Guid)))
            {
                _onSelected?.Invoke(audioItem.Guid, audioItem.SoundId);
            }
        }
    }
}
