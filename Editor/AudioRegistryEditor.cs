using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Yasin.AudioSystem.Editor
{
    /// <summary>
    /// Custom Inspector for AudioRegistry showing PreloadedAssets status and one-click catalog synchronization.
    /// </summary>
    [CustomEditor(typeof(AudioRegistry))]
    public sealed class AudioRegistryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            AudioRegistry registry = (AudioRegistry)target;
            bool isPreloaded = AudioRegistryUtility.IsInPreloadedAssets(registry);

            if (isPreloaded)
            {
                EditorGUILayout.HelpBox(
                    "✅ This AudioRegistry asset is registered in PlayerSettings > Preloaded Assets.\n" +
                    "All AudioCatalog assets (regardless of folder) will load automatically in Player Builds.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "⚠️ This AudioRegistry is not yet in PlayerSettings > Preloaded Assets. Add it so audio catalogs load in Player Builds.",
                    MessageType.Warning);

                if (GUILayout.Button("📌 Add to Preloaded Assets", GUILayout.Height(26f)))
                {
                    AudioRegistryUtility.EnsurePreloaded(registry);
                }
            }

            EditorGUILayout.Space(4f);

            if (GUILayout.Button("🔍 Sync All Catalogs in Project", GUILayout.Height(30f)))
            {
                AudioRegistryUtility.SyncAllCatalogs(createRegistryIfCatalogsExist: true, saveAssets: true);
                serializedObject.Update();
            }

            EditorGUILayout.Space(6f);
            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// Manages the AudioRegistry asset, maintains PreloadedAssets registration, and repairs cross-catalog GUID collisions.
    /// </summary>
    public static class AudioRegistryUtility
    {
        public static AudioRegistry FindRegistry()
        {
            UnityEngine.Object[] preloaded = PlayerSettings.GetPreloadedAssets();
            if (preloaded != null)
            {
                for (int i = 0; i < preloaded.Length; i++)
                {
                    if (preloaded[i] is AudioRegistry reg && reg != null)
                        return reg;
                }
            }

            string[] guids = AssetDatabase.FindAssets("t:AudioRegistry");
            if (guids != null)
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    AudioRegistry reg = AssetDatabase.LoadAssetAtPath<AudioRegistry>(path);
                    if (reg != null)
                        return reg;
                }
            }

            return null;
        }

        public static bool IsInPreloadedAssets(AudioRegistry registry)
        {
            if (registry == null)
                return false;

            UnityEngine.Object[] preloaded = PlayerSettings.GetPreloadedAssets();
            if (preloaded == null)
                return false;

            for (int i = 0; i < preloaded.Length; i++)
            {
                if (preloaded[i] == registry)
                    return true;
            }

            return false;
        }

        public static void EnsurePreloaded(AudioRegistry registry)
        {
            if (registry == null)
                return;

            UnityEngine.Object[] current = PlayerSettings.GetPreloadedAssets();
            bool hasRegistry = false;
            bool hasNull = false;

            if (current != null)
            {
                for (int i = 0; i < current.Length; i++)
                {
                    if (current[i] == null)
                    {
                        hasNull = true;
                    }
                    else if (current[i] == registry)
                    {
                        hasRegistry = true;
                    }
                }
            }

            if (hasRegistry && !hasNull)
                return;

            List<UnityEngine.Object> updated = new List<UnityEngine.Object>(current != null ? current.Length + 1 : 1);
            if (current != null)
            {
                for (int i = 0; i < current.Length; i++)
                {
                    if (current[i] != null)
                    {
                        updated.Add(current[i]);
                    }
                }
            }

            if (!hasRegistry)
            {
                updated.Add(registry);
            }

            PlayerSettings.SetPreloadedAssets(updated.ToArray());
        }

        public static void EnsureFolderRecursive(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return;

            string normalized = folderPath.Replace('\\', '/').Trim().TrimEnd('/');
            if (AssetDatabase.IsValidFolder(normalized))
                return;

            string[] parts = normalized.Split('/');
            if (parts.Length == 0 || !string.Equals(parts[0], "Assets", StringComparison.Ordinal))
                return;

            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string part = parts[i];
                if (string.IsNullOrEmpty(part))
                    continue;

                string next = $"{current}/{part}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, part);
                }
                current = next;
            }
        }

        public static AudioRegistry EnsureRegistryExists()
        {
            AudioRegistry existing = FindRegistry();
            if (existing != null)
            {
                EnsurePreloaded(existing);
                AudioDatabase.SetRegistry(existing);
                return existing;
            }

            string parentFolder = System.IO.Path.GetDirectoryName(AudioRegistry.DefaultRegistryAssetPath)?.Replace('\\', '/');
            EnsureFolderRecursive(parentFolder);

            AudioRegistry created = ScriptableObject.CreateInstance<AudioRegistry>();
            AssetDatabase.CreateAsset(created, AudioRegistry.DefaultRegistryAssetPath);
            EditorUtility.SetDirty(created);
            AssetDatabase.SaveAssets();

            EnsurePreloaded(created);
            AudioDatabase.SetRegistry(created);
            return created;
        }

        public static void SyncAllCatalogs(
            bool createRegistryIfCatalogsExist = true,
            bool saveAssets = false,
            HashSet<string> newlyImportedPaths = null)
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioCatalog");
            List<AudioCatalog> existingPass = new List<AudioCatalog>();
            List<AudioCatalog> newPass = new List<AudioCatalog>();
            List<AudioCatalog> allCatalogs = new List<AudioCatalog>();

            if (guids != null)
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    AudioCatalog cat = AssetDatabase.LoadAssetAtPath<AudioCatalog>(assetPath);
                    if (cat == null)
                        continue;

                    allCatalogs.Add(cat);
                    if (newlyImportedPaths != null && newlyImportedPaths.Contains(assetPath))
                    {
                        newPass.Add(cat);
                    }
                    else
                    {
                        existingPass.Add(cat);
                    }
                }
            }

            HashSet<string> globalGuids = new HashSet<string>(StringComparer.Ordinal);
            bool anyDirty = RepairDuplicateGuids(existingPass, globalGuids);
            if (RepairDuplicateGuids(newPass, globalGuids))
            {
                anyDirty = true;
            }

            AudioRegistry registry = FindRegistry();
            if (registry == null && createRegistryIfCatalogsExist && allCatalogs.Count > 0)
            {
                registry = EnsureRegistryExists();
                anyDirty = true;
            }

            if (registry != null)
            {
                EnsurePreloaded(registry);
                AudioDatabase.SetRegistry(registry);
                if (registry.SyncCatalogs(allCatalogs))
                {
                    EditorUtility.SetDirty(registry);
                    anyDirty = true;
                }
            }

            AudioDatabase.InvalidateLookup();
            AudioKeyDrawer.InvalidateCache();

            if (saveAssets && anyDirty)
            {
                AssetDatabase.SaveAssets();
            }
        }

        private static bool RepairDuplicateGuids(List<AudioCatalog> catalogList, HashSet<string> globalGuids)
        {
            bool anyModified = false;
            for (int c = 0; c < catalogList.Count; c++)
            {
                AudioCatalog catalog = catalogList[c];
                if (catalog == null || catalog.Entries == null)
                    continue;

                bool modified = false;
                IReadOnlyList<AudioCatalog.Entry> entries = catalog.Entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    AudioCatalog.Entry entry = entries[i];
                    if (entry == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(entry.guid) || !globalGuids.Add(entry.guid))
                    {
                        entry.guid = Guid.NewGuid().ToString("N");
                        globalGuids.Add(entry.guid);
                        modified = true;
                    }
                }

                if (modified)
                {
                    anyModified = true;
                    catalog.RebuildLookups();
                    EditorUtility.SetDirty(catalog);
                    Debug.Log($"[AudioSystem] Automatically regenerated colliding/duplicated audio GUIDs inside '{catalog.name}'.", catalog);
                }
            }
            return anyModified;
        }
    }

    [InitializeOnLoad]
    internal static class AudioEditorBootstrap
    {
        static AudioEditorBootstrap()
        {
            EditorApplication.delayCall += RunInitialSync;
        }

        private static void RunInitialSync()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RunInitialSync;
                return;
            }

            AudioRegistryUtility.SyncAllCatalogs(createRegistryIfCatalogsExist: true, saveAssets: false);
        }
    }

    /// <summary>
    /// Automatically invalidates caches and schedules a delayed AudioRegistry sync when AudioCatalog assets are created, duplicated, deleted, or moved.
    /// </summary>
    public sealed class AudioAssetPostprocessor : AssetPostprocessor
    {
        private static readonly HashSet<string> _pendingNewCatalogPaths = new HashSet<string>(StringComparer.Ordinal);
        private static bool _syncScheduled;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool affected = false;

            if (importedAssets != null)
            {
                for (int i = 0; i < importedAssets.Length; i++)
                {
                    string path = importedAssets[i];
                    if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                        continue;

                    Type assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                    if (assetType == typeof(AudioCatalog))
                    {
                        _pendingNewCatalogPaths.Add(path);
                        affected = true;
                    }
                    else if (assetType == typeof(AudioRegistry))
                    {
                        affected = true;
                    }
                }
            }

            if (!affected && deletedAssets != null)
            {
                for (int i = 0; i < deletedAssets.Length; i++)
                {
                    if (deletedAssets[i].EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                    {
                        affected = true;
                        break;
                    }
                }
            }

            if (!affected && movedAssets != null)
            {
                for (int i = 0; i < movedAssets.Length; i++)
                {
                    if (movedAssets[i].EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                    {
                        Type assetType = AssetDatabase.GetMainAssetTypeAtPath(movedAssets[i]);
                        if (assetType == typeof(AudioCatalog) || assetType == typeof(AudioRegistry))
                        {
                            affected = true;
                            break;
                        }
                    }
                }
            }

            if (affected)
            {
                AudioDatabase.InvalidateLookup();
                AudioKeyDrawer.InvalidateCache();
                if (!_syncScheduled)
                {
                    _syncScheduled = true;
                    EditorApplication.delayCall += FlushPendingSync;
                }
            }
        }

        private static void FlushPendingSync()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += FlushPendingSync;
                return;
            }

            _syncScheduled = false;
            HashSet<string> importedBatch = _pendingNewCatalogPaths.Count > 0
                ? new HashSet<string>(_pendingNewCatalogPaths, StringComparer.Ordinal)
                : null;
            _pendingNewCatalogPaths.Clear();

            AudioRegistryUtility.SyncAllCatalogs(
                createRegistryIfCatalogsExist: true,
                saveAssets: false,
                newlyImportedPaths: importedBatch);
        }
    }

    /// <summary>
    /// Ensures all AudioCatalog assets are synced into AudioRegistry and PreloadedAssets before every Player Build.
    /// </summary>
    public sealed class AudioBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            AudioRegistryUtility.SyncAllCatalogs(createRegistryIfCatalogsExist: true, saveAssets: true);

            string[] catalogGuids = AssetDatabase.FindAssets("t:AudioCatalog");
            if (catalogGuids != null && catalogGuids.Length > 0)
            {
                AudioRegistry registry = AudioRegistryUtility.FindRegistry();
                if (registry == null || !AudioRegistryUtility.IsInPreloadedAssets(registry))
                {
                    throw new BuildFailedException("[AudioSystem] Failed to register AudioRegistry in PlayerSettings > Preloaded Assets.");
                }
            }
        }
    }
}
