using BepInEx.Unity.IL2CPP.Utils;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;

namespace TheBetterRoles
{
    public static class CustomSoundsManager
    {
        private static Dictionary<string, AudioClip> soundEffects = new();

        public static void Load()
        {
            soundEffects = new Dictionary<string, AudioClip>();
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();
            foreach (string resourceName in resourceNames)
            {
                if (resourceName.Contains("TheBetterRoles.Resources.Sounds.") && resourceName.Contains(".raw"))
                {
                    soundEffects.Add(resourceName, Utils.loadAudioClipFromResources(resourceName));
                    Logger.Log($"Loaded AudioClip: {resourceName}");
                }
            }
        }

        public static AudioClip? Pet(string name)
        {
            if (!name.Contains('.')) name = "TheBetterRoles.Resources.Sounds." + name + ".raw";
            return soundEffects.TryGetValue(name, out AudioClip? returnValue) ? returnValue : null;
        }

        private static AudioSource? PlaySound(AudioClip clip, bool loop, float volume = 1f, AudioMixerGroup? audioMixer = null)
        {
            if (clip == null)
            {
                Logger.Error("Missing audio clip");
                return null;
            }

            audioMixer ??= (loop ? SoundManager.instance.MusicChannel : SoundManager.instance.SfxChannel);

            // Handle volume duplication logic only if needed
            if (volume > 1f)
            {
                int numberOfSources = Mathf.CeilToInt(volume);
                for (int i = 0; i < numberOfSources; i++)
                {
                    PlayTemporarySound(clip, loop, 1f, audioMixer);
                }
                return null; // No single audio source returned for duplicates
            }

            AudioSource audioSource;
            if (SoundManager.instance.allSources.TryGetValue(clip, out audioSource))
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.volume = volume;
                    audioSource.outputAudioMixerGroup = audioMixer;
                    audioSource.loop = loop;
                    audioSource.Play();
                }
            }
            else
            {
                audioSource = SoundManager.instance.gameObject.AddComponent<AudioSource>();
                audioSource.outputAudioMixerGroup = audioMixer;
                audioSource.playOnAwake = false;
                audioSource.volume = volume;
                audioSource.loop = loop;
                audioSource.clip = clip;
                audioSource.Play();
                SoundManager.instance.allSources.Add(clip, audioSource);
            }

            return audioSource;
        }

        private static void PlayTemporarySound(AudioClip clip, bool loop, float volume, AudioMixerGroup? audioMixer)
        {
            AudioSource tempAudioSource = SoundManager.instance.gameObject.AddComponent<AudioSource>();
            tempAudioSource.outputAudioMixerGroup = audioMixer;
            tempAudioSource.playOnAwake = false;
            tempAudioSource.volume = volume;
            tempAudioSource.loop = loop;
            tempAudioSource.clip = clip;
            tempAudioSource.Play();

            if (!loop)
            {
                SoundManager.instance.StartCoroutine(DestroyAfterPlayback(tempAudioSource, clip.length));
            }
        }

        private static IEnumerator DestroyAfterPlayback(AudioSource audioSource, float clipLength)
        {
            yield return new WaitForSeconds(clipLength);
            UnityEngine.Object.Destroy(audioSource);
        }

        public static void Play(string name, float volume = 1f, bool loop = false)
        {
            AudioClip? clipToPlay = Pet(name);
            Stop(name);
            if (Constants.ShouldPlaySfx() && clipToPlay != null)
            {
                AudioSource? source = PlaySound(clipToPlay, loop, volume);
                if (source != null)
                {
                    source.loop = loop;
                }
            }
        }

        public static void PlayDynamic(string path, Vector2 position, float range = 10f, float volume = 1f, bool loop = false)
        {
            AudioClip? clipToPlay = Pet(path);
            if (clipToPlay != null && Constants.ShouldPlaySfx())
            {
                GameObject soundObject = new("DynamicSound");
                soundObject.transform.position = position;

                AudioSource audioSource = soundObject.AddComponent<AudioSource>();
                audioSource.clip = clipToPlay;
                audioSource.volume = volume;
                audioSource.loop = loop;

                audioSource.spatialBlend = 1f;
                audioSource.maxDistance = range;
                audioSource.minDistance = range / 2f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;

                audioSource.Play();

                if (!loop)
                {
                    SoundManager.instance.StartCoroutine(ManageDynamicSound(soundObject, audioSource, position, range));
                }
            }
            else
            {
                Logger.Error($"Unable to play dynamic sound: {path}");
            }
        }

        private static IEnumerator ManageDynamicSound(GameObject soundObject, AudioSource audioSource, Vector2 position, float range)
        {
            while (audioSource.isPlaying)
            {
                UpdateVolumeBasedOnDistance(audioSource, position, range);
                yield return null;
            }

            UnityEngine.Object.Destroy(soundObject);
        }

        private static void UpdateVolumeBasedOnDistance(AudioSource audioSource, Vector2 sourcePosition, float range)
        {
            Vector2 playerPosition = PlayerControl.LocalPlayer.GetCustomPosition();
            float distance = Vector2.Distance(playerPosition, sourcePosition);

            float clampedDistance = Mathf.Clamp(distance, 0f, range);
            float volume = Mathf.Lerp(1f, 0f, Mathf.InverseLerp(range / 2f, range, clampedDistance));

            audioSource.volume = volume;
        }

        public static void Stop(string path)
        {
            AudioClip? soundToStop = Pet(path);
            if (soundToStop != null && Constants.ShouldPlaySfx())
            {
                SoundManager.Instance.StopSound(soundToStop);
            }
        }

        public static void StopAll()
        {
            if (soundEffects == null) return;
            foreach (var path in soundEffects.Keys) Stop(path);
        }
    }
}
