using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yasin.AudioSystem.Editor
{
    /// <summary>
    /// Custom Inspector for AudioCatalog ScriptableObjects providing catalog summaries, duplicate ID warnings, and Edit Mode audio previews.
    /// </summary>
    [CustomEditor(typeof(AudioCatalog))]
    public sealed class AudioCatalogEditor : UnityEditor.Editor
    {
        private SerializedProperty _defaultMixerProp;
        private SerializedProperty _entriesProp;
        private readonly List<string> _duplicateWarnings = new List<string>();
        private bool _showPreviewBar = true;

        private void OnEnable()
        {
            _defaultMixerProp = serializedObject.FindProperty("defaultMixerGroup");
            _entriesProp = serializedObject.FindProperty("entries");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AudioCatalog catalog = (AudioCatalog)target;

            DrawHeader(catalog);
            DrawDuplicateWarnings(catalog);
            EditorGUILayout.Space(4f);

            if (_defaultMixerProp != null)
            {
                EditorGUILayout.PropertyField(_defaultMixerProp);
            }

            EditorGUILayout.Space(4f);

            if (_entriesProp != null)
            {
                EditorGUILayout.PropertyField(_entriesProp, true);
            }

            EditorGUILayout.Space(6f);
            DrawQuickPreviewSection(catalog);

            if (serializedObject.ApplyModifiedProperties())
            {
                catalog.SanitizeEntries();
                catalog.RebuildLookups();
                AudioDatabase.InvalidateLookup();
                AudioKeyDrawer.InvalidateCache();
            }
        }

        private void DrawHeader(AudioCatalog catalog)
        {
            int count = catalog.Entries != null ? catalog.Entries.Count : 0;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"🎵 {catalog.name}  ({count} Sounds)", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("⏹ Stop Preview", EditorStyles.miniButton, GUILayout.Width(110f)))
                {
                    AudioPreviewUtility.StopPreview();
                }
            }
        }

        private void DrawDuplicateWarnings(AudioCatalog catalog)
        {
            _duplicateWarnings.Clear();
            if (catalog.Entries == null || catalog.Entries.Count == 0)
                return;

            HashSet<string> localIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> otherCatalogIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string[] guids = AssetDatabase.FindAssets("t:AudioCatalog");
            if (guids != null)
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    AudioCatalog other = AssetDatabase.LoadAssetAtPath<AudioCatalog>(path);
                    if (other == null || other == catalog || other.Entries == null)
                        continue;

                    for (int j = 0; j < other.Entries.Count; j++)
                    {
                        var e = other.Entries[j];
                        if (e != null && !string.IsNullOrWhiteSpace(e.id))
                        {
                            otherCatalogIds.Add(e.id.Trim());
                        }
                    }
                }
            }

            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                var entry = catalog.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                    continue;

                string id = entry.id.Trim();
                if (!localIds.Add(id))
                {
                    _duplicateWarnings.Add($"• Duplicate ID within this catalog: '{id}'");
                }
                else if (otherCatalogIds.Contains(id))
                {
                    _duplicateWarnings.Add($"• ID also defined in another catalog: '{id}'");
                }
            }

            if (_duplicateWarnings.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    "⚠️ Duplicate Sound IDs Detected:\n" + string.Join("\n", _duplicateWarnings),
                    MessageType.Warning);
            }
        }

        private void DrawQuickPreviewSection(AudioCatalog catalog)
        {
            if (catalog.Entries == null || catalog.Entries.Count == 0)
                return;

            _showPreviewBar = EditorGUILayout.Foldout(_showPreviewBar, "🔊 Quick Sound Preview (Edit Mode)", true);
            if (!_showPreviewBar)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (int i = 0; i < catalog.Entries.Count; i++)
                {
                    AudioCatalog.Entry entry = catalog.Entries[i];
                    if (entry == null)
                        continue;

                    string displayId = string.IsNullOrWhiteSpace(entry.id) ? $"(Unnamed #{i})" : entry.id;
                    var asset = entry.ResolveResource();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(displayId, EditorStyles.miniLabel);
                        using (new EditorGUI.DisabledScope(asset == null))
                        {
                            if (GUILayout.Button("▶", EditorStyles.miniButton, GUILayout.Width(28f)))
                            {
                                AudioPreviewUtility.PlayPreview(asset);
                            }
                        }
                    }
                }
            }
        }
    }
}
