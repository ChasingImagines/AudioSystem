using System;
using UnityEngine;

namespace Yasin.AudioSystem
{
    /// <summary>
    /// Generation-checked handle for controlling an active pooled sound playback safely.
    /// Once the emitter returns to the pool, calls through stale handles are safely ignored.
    /// </summary>
    public readonly struct SoundHandle : IEquatable<SoundHandle>
    {
        public static readonly SoundHandle Invalid = default;

        private readonly SoundEmitter _emitter;
        private readonly uint _generation;

        internal SoundHandle(SoundEmitter emitter, uint generation)
        {
            _emitter = emitter;
            _generation = generation;
        }

        public bool IsValid => _emitter != null && _emitter.IsGenerationActive(_generation);
        public bool IsPlaying => IsValid && _emitter.IsPlaying;

        public void Stop()
        {
            if (IsValid)
            {
                _emitter.Stop();
            }
        }

        public void Pause()
        {
            if (IsValid)
            {
                _emitter.Pause();
            }
        }

        public void Resume()
        {
            if (IsValid)
            {
                _emitter.Resume();
            }
        }

        public void SetVolume(float volume)
        {
            if (IsValid)
            {
                _emitter.SetVolume(volume);
            }
        }

        public void SetPitch(float pitch)
        {
            if (IsValid)
            {
                _emitter.SetPitch(pitch);
            }
        }

        public void SetPosition(Vector3 position)
        {
            if (IsValid)
            {
                _emitter.SetPosition(position);
            }
        }

        public SoundHandle Follow(Transform target)
        {
            if (IsValid)
            {
                _emitter.Follow(target);
            }
            return this;
        }

        public bool Equals(SoundHandle other) => ReferenceEquals(_emitter, other._emitter) && _generation == other._generation;
        public override bool Equals(object obj) => obj is SoundHandle other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = _emitter != null ? _emitter.GetHashCode() : 0;
                return (h * 397) ^ (int)_generation;
            }
        }

        public static bool operator ==(SoundHandle a, SoundHandle b) => a.Equals(b);
        public static bool operator !=(SoundHandle a, SoundHandle b) => !a.Equals(b);
    }
}
