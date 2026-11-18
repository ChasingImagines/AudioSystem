using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yasin.AudioSystem.Editor
{
    /// <summary>
    /// AudioKey alanlarını ve [AudioId] etiketli string alanları Inspector'da arama destekli şık bir açılır menü ile çizer.
    /// </summary>
    [CustomPropertyDrawer(typeof(AudioKey))]
    [CustomPropertyDrawer(typeof(AudioIdAttribute))]
    public sealed class AudioKeyDrawer : PropertyDrawer
    {
        private static readonly List<string> CachedSoundIds = new List<string>();
        private static double _lastCacheTime;
        private static AdvancedDropdownState _dropdownState;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty targetStringProp = property;
            if (property.type == nameof(AudioKey))
            {
                targetStringProp = property.FindPropertyRelative("soundId");
            }
            else if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EnsureCatalogLoaded();

            label = EditorGUI.BeginProperty(position, label, property);
            Rect contentRect = EditorGUI.PrefixLabel(position, label);

            string currentId = targetStringProp != null ? targetStringProp.stringValue : string.Empty;
            bool isEmpty = string.IsNullOrWhiteSpace(currentId);
            bool isValid = !isEmpty && CachedSoundIds.Contains(currentId);

            string buttonText;
            if (isEmpty)
            {
                buttonText = "— (Ses Seçin) —";
            }
            else if (isValid)
            {
                buttonText = $"🔊 {currentId}";
            }
            else
            {
                buttonText = $"⚠️ {currentId} (Katalogda Yok!)";
            }

            Color oldBg = GUI.backgroundColor;
            if (!isEmpty && !isValid)
            {
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            }
            else if (isValid)
            {
                GUI.backgroundColor = new Color(0.45f, 0.85f, 0.55f);
            }

            if (GUI.Button(contentRect, buttonText, EditorStyles.popup))
            {
                if (_dropdownState == null)
                    _dropdownState = new AdvancedDropdownState();

                var dropdown = new AudioIdDropdown(_dropdownState, CachedSoundIds, selectedId =>
                {
                    targetStringProp.serializedObject.Update();
                    targetStringProp.stringValue = selectedId;
                    targetStringProp.serializedObject.ApplyModifiedProperties();
                });

                dropdown.Show(contentRect);
            }

            GUI.backgroundColor = oldBg;
            EditorGUI.EndProperty();
        }

        private static void EnsureCatalogLoaded()
        {
            if (CachedSoundIds.Count > 0 && EditorApplication.timeSinceStartup - _lastCacheTime < 2.0)
                return;

            _lastCacheTime = EditorApplication.timeSinceStartup;
            CachedSoundIds.Clear();

            string[] guids = AssetDatabase.FindAssets("t:AudioCatalog");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AudioCatalog catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(path);
                if (catalog == null) continue;

                IReadOnlyList<AudioCatalog.Entry> entries = catalog.Entries;
                for (int j = 0; j < entries.Count; j++)
                {
                    AudioCatalog.Entry entry = entries[j];
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.id))
                    {
                        string id = entry.id.Trim();
                        if (!CachedSoundIds.Contains(id))
                        {
                            CachedSoundIds.Add(id);
                        }
                    }
                }
            }

            CachedSoundIds.Sort(StringComparer.OrdinalIgnoreCase);
        }
    }
}
