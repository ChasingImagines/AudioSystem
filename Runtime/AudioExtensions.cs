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
        /// [0, 1] aralığındaki lineer slider değerini Unity AudioMixer için logaritmik desibel (-80 dB .. 0 dB) değerine dönüştürür.
        /// </summary>
        public static float ToLogarithmicVolume(this float sliderValue)
        {
            return Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;
        }

        /// <summary>
        /// [0, 1] aralığındaki geçiş oranını insan kulağının algısına uygun logaritmik [0, 1] ölçeğine dönüştürür.
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
