using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
#if UNITY_6000_0_OR_NEWER
using AudioAsset = UnityEngine.Audio.AudioResource;
#else
using AudioAsset = UnityEngine.AudioClip;
#endif

namespace Yasin.AudioSystem.Editor
{
    /// <summary>
    /// AudioKey alanlarını ve [AudioId(filterPrefix)] etiketli string/AudioKey alanlarını Inspector'da
    /// prefix filtreli açılır menü, GUID/case-insensitive doğrulama ve tek tıkla önizleme butonu ile çizer.
    /// </summary>
    [CustomPropertyDrawer(typeof(AudioKey))]
    [CustomPropertyDrawer(typeof(AudioIdAttribute))]
    public sealed class AudioKeyDrawer : PropertyDrawer
    {
        private static readonly List<AudioEntryOption> CachedEntries = new List<AudioEntryOption>();
        private static readonly Dictionary<string, AudioEntryOption> CachedByGuid = new Dictionary<string, AudioEntryOption>(StringComparer.Ordinal);
        private static readonly Dictionary<string, AudioEntryOption> CachedById = new Dictionary<string, AudioEntryOption>(StringComparer.OrdinalIgnoreCase);
        private static bool _cacheValid;
        private static AdvancedDropdownState _dropdownState;

        public static void InvalidateCache()
        {
            _cacheValid = false;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool isAudioKey = property.type == nameof(AudioKey);
            SerializedProperty guidProp = isAudioKey ? property.FindPropertyRelative("guid") : null;
            SerializedProperty idProp = isAudioKey ? property.FindPropertyRelative("soundId") : property;

            if (!isAudioKey && property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EnsureCatalogLoaded();

            AudioIdAttribute idAttr = attribute as AudioIdAttribute;
            if (idAttr == null && fieldInfo != null)
            {
                object[] attrs = fieldInfo.GetCustomAttributes(typeof(AudioIdAttribute), true);
                if (attrs != null && attrs.Length > 0)
                {
                    idAttr = attrs[0] as AudioIdAttribute;
                }
            }
            string filterPrefix = idAttr != null ? idAttr.FilterPrefix : null;

            label = EditorGUI.BeginProperty(position, label, property);
            Rect contentRect = EditorGUI.PrefixLabel(position, label);

            const float previewBtnWidth = 24f;
            const float spacing = 3f;
            Rect dropdownRect = new Rect(contentRect.x, contentRect.y, Mathf.Max(40f, contentRect.width - previewBtnWidth - spacing), contentRect.height);
            Rect previewRect = new Rect(dropdownRect.xMax + spacing, contentRect.y, previewBtnWidth, contentRect.height);

            bool hasMixed = (idProp != null && idProp.hasMultipleDifferentValues) ||
                            (guidProp != null && guidProp.hasMultipleDifferentValues);

            string currentGuid = !hasMixed && guidProp != null ? guidProp.stringValue : string.Empty;
            string currentId = !hasMixed && idProp != null ? idProp.stringValue : string.Empty;

            bool isEmpty = !hasMixed && string.IsNullOrWhiteSpace(currentGuid) && string.IsNullOrWhiteSpace(currentId);
            bool isValid = false;
            AudioEntryOption resolved = default;

            if (!hasMixed && !isEmpty)
            {
                if (isAudioKey && !string.IsNullOrWhiteSpace(currentGuid) && CachedByGuid.TryGetValue(currentGuid.Trim(), out resolved))
                {
                    isValid = true;
                }
                else if (!string.IsNullOrWhiteSpace(currentId) && CachedById.TryGetValue(currentId.Trim(), out resolved))
                {
                    isValid = true;
                }
            }

            string buttonText;
            if (hasMixed)
            {
                buttonText = "— (Karışık Değerler) —";
            }
            else if (isEmpty)
            {
                buttonText = string.IsNullOrEmpty(filterPrefix)
                    ? "— (Ses Seçin) —"
                    : $"— (Ses Seçin: {filterPrefix}*) —";
            }
            else if (isValid)
            {
                buttonText = $"🔊 {resolved.Id}";
            }
            else
            {
                string fallbackLabel = !string.IsNullOrWhiteSpace(currentId) ? currentId : currentGuid;
                buttonText = $"⚠️ {fallbackLabel} (Katalogda Yok!)";
            }

            Color oldBg = GUI.backgroundColor;
            if (hasMixed)
            {
                GUI.backgroundColor = new Color(0.95f, 0.9f, 0.7f);
            }
            else if (!isEmpty && !isValid)
            {
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            }
            else if (isValid)
            {
                GUI.backgroundColor = new Color(0.45f, 0.85f, 0.55f);
            }

            if (GUI.Button(dropdownRect, buttonText, EditorStyles.popup))
            {
                if (_dropdownState == null)
                    _dropdownState = new AdvancedDropdownState();

                SerializedObject targetSO = property.serializedObject;
                string propPath = property.propertyPath;
                bool targetIsKey = isAudioKey;

                AudioIdDropdown dropdown = new AudioIdDropdown(
                    _dropdownState,
                    CachedEntries,
                    filterPrefix,
                    (selectedGuid, selectedId) =>
                    {
                        targetSO.Update();
                        SerializedProperty root = targetSO.FindProperty(propPath);
                        if (root != null)
                        {
                            if (targetIsKey)
                            {
                                SerializedProperty gProp = root.FindPropertyRelative("guid");
                                SerializedProperty sProp = root.FindPropertyRelative("soundId");
                                if (gProp != null) gProp.stringValue = selectedGuid ?? string.Empty;
                                if (sProp != null) sProp.stringValue = selectedId ?? string.Empty;
                            }
                            else if (root.propertyType == SerializedPropertyType.String)
                            {
                                root.stringValue = selectedId ?? string.Empty;
                            }
                            targetSO.ApplyModifiedProperties();
                        }
                    });

                dropdown.Show(dropdownRect);
            }

            GUI.backgroundColor = oldBg;

            AudioAsset previewAsset = isValid ? resolved.Asset as AudioAsset : null;
            using (new EditorGUI.DisabledScope(!isValid || previewAsset == null))
            {
                if (GUI.Button(previewRect, new GUIContent("▶", "Editörde Sesi Dinle (Önizle)")))
                {
                    AudioPreviewUtility.PlayPreview(previewAsset);
                }
            }

            EditorGUI.EndProperty();
        }

        private static void EnsureCatalogLoaded()
        {
            if (_cacheValid)
                return;

            _cacheValid = true;
            CachedEntries.Clear();
            CachedByGuid.Clear();
            CachedById.Clear();

            string[] guids = AssetDatabase.FindAssets("t:AudioCatalog");
            if (guids != null)
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    AudioCatalog catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(path);
                    if (catalog == null || catalog.Entries == null)
                        continue;

                    IReadOnlyList<AudioCatalog.Entry> entries = catalog.Entries;
                    for (int j = 0; j < entries.Count; j++)
                    {
                        AudioCatalog.Entry entry = entries[j];
                        if (entry == null)
                            continue;

                        string entryId = entry.id != null ? entry.id.Trim() : string.Empty;
                        string entryGuid = entry.guid != null ? entry.guid.Trim() : string.Empty;
                        if (string.IsNullOrEmpty(entryId) && string.IsNullOrEmpty(entryGuid))
                            continue;

                        AudioEntryOption option = new AudioEntryOption(
                            entryGuid,
                            entryId,
                            catalog.name,
                            entry.ResolveResource());

                        CachedEntries.Add(option);

                        if (!string.IsNullOrEmpty(entryGuid) && !CachedByGuid.ContainsKey(entryGuid))
                        {
                            CachedByGuid[entryGuid] = option;
                        }

                        if (!string.IsNullOrEmpty(entryId) && !CachedById.ContainsKey(entryId))
                        {
                            CachedById[entryId] = option;
                        }
                    }
                }
            }

            CachedEntries.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
