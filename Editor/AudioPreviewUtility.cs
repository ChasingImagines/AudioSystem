using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
#if UNITY_6000_0_OR_NEWER
using AudioAsset = UnityEngine.Audio.AudioResource;
#else
using AudioAsset = UnityEngine.AudioClip;
#endif

namespace Yasin.AudioSystem.Editor
{
    /// <summary>
    /// Reflection-based helper for previewing AudioClips inside the Unity Editor outside of Play Mode.
    /// </summary>
    public static class AudioPreviewUtility
    {
        private static Type _audioUtilType;
        private static MethodInfo _playClip3Arg;
        private static MethodInfo _playClip1Arg;
        private static MethodInfo _stopAllClips;
        private static bool _resolved;

        private static void EnsureReflection()
        {
            if (_resolved)
                return;

            _resolved = true;
            try
            {
                _audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
                if (_audioUtilType == null)
                    return;

                const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                _playClip3Arg = _audioUtilType.GetMethod("PlayPreviewClip", flags, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)
                                ?? _audioUtilType.GetMethod("PlayClip", flags, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);

                _playClip1Arg = _audioUtilType.GetMethod("PlayPreviewClip", flags, null, new[] { typeof(AudioClip) }, null)
                                ?? _audioUtilType.GetMethod("PlayClip", flags, null, new[] { typeof(AudioClip) }, null);

                _stopAllClips = _audioUtilType.GetMethod("StopAllPreviewClips", flags, null, Type.EmptyTypes, null)
                                ?? _audioUtilType.GetMethod("StopAllClips", flags, null, Type.EmptyTypes, null);
            }
            catch
            {
                // Ignore cleanly across Unity Editor version changes
            }
        }

        public static void PlayPreview(AudioAsset asset)
        {
            if (asset == null)
                return;

            EnsureReflection();
            StopPreview();

            AudioClip clip = asset as AudioClip;
            if (clip == null)
            {
                Debug.Log($"[AudioSystem] '{asset.name}' is not an AudioClip ({asset.GetType().Name}); direct Edit Mode preview is only supported for AudioClip assets.", asset);
                return;
            }

            try
            {
                if (_playClip3Arg != null)
                {
                    _playClip3Arg.Invoke(null, new object[] { clip, 0, false });
                }
                else if (_playClip1Arg != null)
                {
                    _playClip1Arg.Invoke(null, new object[] { clip });
                }
            }
            catch
            {
                // Ignore across Editor versions
            }
        }

        public static void StopPreview()
        {
            EnsureReflection();
            try
            {
                _stopAllClips?.Invoke(null, null);
            }
            catch
            {
                // Ignore
            }
        }
    }
}
