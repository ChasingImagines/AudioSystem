using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yasin.AudioSystem.Editor
{
    /// <summary>
    /// [AudioId] özniteliğine sahip string alanları Inspector'da çizerek:
    /// - AudioCatalog.asset içindeki tüm ses ID'lerini otomatik tarar,
    /// - Fuzzy search (arama çubuklu) ve kategorilendirilmiş AdvancedDropdown menüsü sunar,
    /// - Geçerli sesler için yeşil/mavi durum, geçersiz/boş ID'ler için belirgin uyarı gösterir.
    /// </summary>
    [CustomPropertyDrawer(typeof(AudioIdAttribute))]
    public sealed class AudioIdDrawer : PropertyDrawer
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
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EnsureCatalogLoaded();

            label = EditorGUI.BeginProperty(position, label, property);
            Rect contentRect = EditorGUI.PrefixLabel(position, label);

            string currentId = property.stringValue;
            bool isEmpty = string.IsNullOrWhiteSpace(currentId);
            bool isValid = !isEmpty && CachedSoundIds.Contains(currentId);

            // Sağ tarafta dropdown butonu
            float buttonWidth = contentRect.width;
            Rect buttonRect = new Rect(contentRect.x, contentRect.y, buttonWidth, contentRect.height);

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

            Color prevBg = GUI.backgroundColor;
            if (!isEmpty && !isValid)
            {
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f, 1f);
            }
            else if (isValid)
            {
                GUI.backgroundColor = new Color(0.7f, 1f, 0.7f, 1f);
            }

            if (GUI.Button(buttonRect, EditorGUIUtility.TrTempContent(buttonText), EditorStyles.popup))
            {
                _dropdownState ??= new AdvancedDropdownState();
                var dropdown = new AudioIdDropdown(_dropdownState, CachedSoundIds, selectedId =>
                {
                    property.serializedObject.Update();
                    property.stringValue = selectedId;
                    property.serializedObject.ApplyModifiedProperties();
                });

                dropdown.Show(buttonRect);
            }

            GUI.backgroundColor = prevBg;
            EditorGUI.EndProperty();
        }

        private static void EnsureCatalogLoaded()
        {
            if (CachedSoundIds.Count > 0 && EditorApplication.timeSinceStartup - _lastCacheTime < 2.0)
                return;

            _lastCacheTime = EditorApplication.timeSinceStartup;
            CachedSoundIds.Clear();

            // 1. AudioCatalog varlığını bul
            string[] guids = AssetDatabase.FindAssets("t:AudioCatalog");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (catalog == null) continue;

                var so = new SerializedObject(catalog);
                var entriesProp = so.FindProperty("entries");
                if (entriesProp != null && entriesProp.isArray)
                {
                    for (int j = 0; j < entriesProp.arraySize; j++)
                    {
                        var entryProp = entriesProp.GetArrayElementAtIndex(j);
                        var idProp = entryProp.FindPropertyRelative("id");
                        if (idProp != null && !string.IsNullOrWhiteSpace(idProp.stringValue))
                        {
                            string id = idProp.stringValue.Trim();
                            if (!CachedSoundIds.Contains(id))
                            {
                                CachedSoundIds.Add(id);
                            }
                        }
                    }
                }
            }

            CachedSoundIds.Sort(StringComparer.OrdinalIgnoreCase);
        }
    }
}

