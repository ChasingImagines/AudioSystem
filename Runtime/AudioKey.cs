using System;
using UnityEngine;

namespace Yasin.AudioSystem
{
    /// <summary>
    /// Inspector-friendly, strongly-typed sound reference key backed by both GUID and readable ID.
    /// Survives sound ID renames in catalogs via GUID while also supporting raw string IDs in code.
    /// </summary>
    [Serializable]
    public struct AudioKey : IEquatable<AudioKey>
    {
        [SerializeField] private string guid;
        [SerializeField] private string soundId;

        public string Guid => guid;
        public string SoundId => soundId;
        public bool IsValid => !string.IsNullOrWhiteSpace(guid) || !string.IsNullOrWhiteSpace(soundId);

        public AudioKey(string soundId)
        {
            this.guid = string.Empty;
            this.soundId = soundId != null ? soundId.Trim() : string.Empty;
        }

        public AudioKey(string guid, string soundId)
        {
            this.guid = guid != null ? guid.Trim() : string.Empty;
            this.soundId = soundId != null ? soundId.Trim() : string.Empty;
        }

        /// <summary>
        /// Plays this sound as a 2D UI sound.
        /// </summary>
        public SoundHandle PlayUI(float volumeScale = 1f, float pitchScale = 1f)
        {
            return AudioSystem.PlayUI(this, volumeScale, pitchScale);
        }

        /// <summary>
        /// Plays this sound as a 3D spatial sound at the given world position.
        /// </summary>
        public SoundHandle Play(
            Vector3 position,
            float volumeScale = 1f,
            float pitchScale = 1f,
            float? spatialBlendOverride = null,
            bool? loopOverride = null)
        {
            return AudioSystem.Play(this, position, volumeScale, pitchScale, spatialBlendOverride, loopOverride);
        }

        /// <summary>
        /// Plays this sound as a 2D UI sound when no position is specified.
        /// </summary>
        public SoundHandle Play()
        {
            return PlayUI();
        }

        /// <summary>
        /// Plays this catalog entry as background music with a logarithmic crossfade.
        /// </summary>
        public void PlayMusic(float fadeDuration = 1f, bool loop = true, float volumeScale = 1f)
        {
            AudioSystem.PlayMusic(this, fadeDuration, loop, volumeScale);
        }

        public bool Equals(AudioKey other)
        {
            if (!string.IsNullOrEmpty(guid) && !string.IsNullOrEmpty(other.guid))
                return string.Equals(guid, other.guid, StringComparison.Ordinal);

            return string.Equals(soundId, other.soundId, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) => obj is AudioKey other && Equals(other);

        public override int GetHashCode()
        {
            if (!string.IsNullOrEmpty(guid))
                return StringComparer.Ordinal.GetHashCode(guid);

            return soundId != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(soundId) : 0;
        }

        public override string ToString() => !string.IsNullOrWhiteSpace(soundId) ? soundId : (IsValid ? guid : "(None)");

        public static bool operator ==(AudioKey a, AudioKey b) => a.Equals(b);
        public static bool operator !=(AudioKey a, AudioKey b) => !a.Equals(b);

        public static implicit operator AudioKey(string id) => new AudioKey(id);
        public static implicit operator string(AudioKey key) => key.SoundId;
    }
}
