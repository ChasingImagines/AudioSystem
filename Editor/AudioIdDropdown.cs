using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yasin.AudioSystem.Editor
{
    /// <summary>
    /// [AudioId] özniteliğine sahip string alanlar için açılır liste ve fuzzy arama sunan dropdown.
    /// </summary>
    public sealed class AudioIdDropdown : AdvancedDropdown
    {
        private sealed class AudioDropdownItem : AdvancedDropdownItem
        {
            public string SoundId { get; }
            public bool IsClearOption { get; }

            public AudioDropdownItem(string name, string soundId, bool isClearOption = false) : base(name)
            {
                SoundId = soundId;
                IsClearOption = isClearOption;
            }
        }

        private readonly IReadOnlyList<string> _soundIds;
        private readonly Action<string> _onSelected;

        public AudioIdDropdown(
            AdvancedDropdownState state,
            IReadOnlyList<string> soundIds,
            Action<string> onSelected) : base(state)
        {
            _soundIds = soundIds;
            _onSelected = onSelected;
            minimumSize = new Vector2(280f, 320f);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Ses Kataloğu (Audio Catalog)");

            root.AddChild(new AudioDropdownItem("None (Boş Bırak)", string.Empty, isClearOption: true));

            if (_soundIds == null || _soundIds.Count == 0)
            {
                root.AddChild(new AdvancedDropdownItem("(Katalogda ses bulunamadı)"));
                return root;
            }

            var groupDict = new Dictionary<string, AdvancedDropdownItem>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _soundIds.Count; i++)
            {
                string id = _soundIds[i];
                if (string.IsNullOrWhiteSpace(id)) continue;

                // "ui_click" -> prefix "sfx_ui", leaf "click" veya prefix "sfx", vb.
                int firstUnder = id.IndexOf('_');
                if (firstUnder > 0)
                {
                    string prefix = id.Substring(0, firstUnder);
                    if (!groupDict.TryGetValue(prefix, out var groupItem))
                    {
                        groupItem = new AdvancedDropdownItem(prefix.ToUpperInvariant());
                        groupDict[prefix] = groupItem;
                        root.AddChild(groupItem);
                    }
                    groupItem.AddChild(new AudioDropdownItem(id, id));
                }
                else
                {
                    root.AddChild(new AudioDropdownItem(id, id));
                }
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);
            if (item is AudioDropdownItem audioItem)
            {
                _onSelected?.Invoke(audioItem.SoundId);
            }
        }
    }
}

