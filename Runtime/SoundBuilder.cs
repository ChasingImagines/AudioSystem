using UnityEngine;
using UnityEngine.Audio;

namespace Yasin.AudioSystem {
    public class SoundBuilder {
        readonly SoundManager soundManager;
        Vector3 position = Vector3.zero;
        bool randomPitch;

        public SoundBuilder(SoundManager soundManager) {
            this.soundManager = soundManager;
        }

        public SoundBuilder WithPosition(Vector3 position) {
            this.position = position;
            return this;
        }

        public SoundBuilder WithRandomPitch() {
            this.randomPitch = true;
            return this;
        }

        public void Play(AudioResource resource, float volume = 1f, float spatialBlend = 1f) {
            if (resource == null) return;
            var data = new SoundData {
                audioResource = resource,
                volume = volume,
                spatialBlend = spatialBlend
            };
            Play(data);
        }

        public void Play(SoundData soundData) {
            if (soundData == null) {
                Debug.LogError("SoundData is null");
                return;
            }
            
            if (!soundManager.CanPlaySound(soundData)) return;
            
            SoundEmitter soundEmitter = soundManager.Get();
            if (soundEmitter == null) {
                Debug.LogError("SoundEmitter alınamadı (pool veya prefab eksik).");
                return;
            }

            soundEmitter.Initialize(soundData);
            soundEmitter.transform.position = position;
            soundEmitter.transform.parent = soundManager.transform;

            if (randomPitch) {
                soundEmitter.WithRandomPitch();
            }

            if (soundData.frequentSound) {
                soundEmitter.Node = soundManager.FrequentSoundEmitters.AddLast(soundEmitter);
            }
            
            soundEmitter.Play();
        }
    }
}