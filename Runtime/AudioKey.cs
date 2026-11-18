using System;
using UnityEngine;

namespace Yasin.AudioSystem
{
    /// <summary>
    /// Inspector dostu, tip güvenli ses referansı anahtarı.
    /// Kodda elle string yazma zorunluluğunu ve yazım hatalarını (typo) tamamen ortadan kaldırır.
    /// </summary>
    [Serializable]
    public struct AudioKey : IEquatable<AudioKey>
    {
        [SerializeField]
        private string soundId;

        public string SoundId => soundId;
        public bool IsValid => !string.IsNullOrWhiteSpace(soundId);

        public AudioKey(string soundId)
        {
            this.soundId = soundId != null ? soundId.Trim() : string.Empty;
        }

        /// <summary>
        /// Bu anahtara bağlı sesi 2D (UI) olarak çalar.
        /// </summary>
        public SoundEmitter PlayUI(float volume = 1f, float pitch = 1f)
        {
            return AudioSystem.PlayUI(soundId, volume, pitch);
        }

        /// <summary>
        /// Bu anahtara bağlı sesi 3D uzamsal olarak dünya pozisyonunda çalar.
        /// </summary>
        public SoundEmitter Play(Vector3 position, float volume = 1f, float spatialBlend = 1f, float pitch = 1f)
        {
            return AudioSystem.Play(soundId, position, volume, spatialBlend, pitch);
        }

        /// <summary>
        /// Konum verilmediğinde varsayılan olarak 2D UI sesi çalar.
        /// </summary>
        public SoundEmitter Play()
        {
            return PlayUI();
        }

        public bool Equals(AudioKey other) => string.Equals(soundId, other.soundId, StringComparison.OrdinalIgnoreCase);
        public override bool Equals(object obj) => obj is AudioKey other && Equals(other);
        public override int GetHashCode() => soundId != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(soundId) : 0;
        public override string ToString() => IsValid ? soundId : "(None)";

        public static bool operator ==(AudioKey a, AudioKey b) => a.Equals(b);
        public static bool operator !=(AudioKey a, AudioKey b) => !a.Equals(b);

        public static implicit operator AudioKey(string id) => new AudioKey(id);
        public static implicit operator string(AudioKey key) => key.SoundId;
    }
}
