using UnityEngine;
#if UNITY_6000_0_OR_NEWER
using AudioAsset = UnityEngine.Audio.AudioResource;
#else
using AudioAsset = UnityEngine.AudioClip;
#endif

namespace Yasin.AudioSystem
{
    public static class AudioExtensions
    {
        /// <summary>
        /// Converts a linear [0, 1] slider value into logarithmic decibels (-80 dB to 0 dB) for Unity AudioMixers.
        /// </summary>
        public static float ToLogarithmicVolume(this float sliderValue)
        {
            return Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
        }

        /// <summary>
        /// Converts a [0, 1] fade fraction into a logarithmic [0, 1] scale matching human loudness perception.
        /// </summary>
        public static float ToLogarithmicFraction(this float fraction)
        {
            return Mathf.Log10(1f + 9f * Mathf.Clamp01(fraction));
        }

        internal static void AssignAsset(this AudioSource source, AudioAsset asset)
        {
            if (source == null)
                return;

#if UNITY_6000_0_OR_NEWER
            source.resource = asset;
#else
            source.clip = asset;
#endif
        }

        internal static AudioAsset GetAssignedAsset(this AudioSource source)
        {
            if (source == null)
                return null;

#if UNITY_6000_0_OR_NEWER
            return source.resource;
#else
            return source.clip;
#endif
        }
    }
}
