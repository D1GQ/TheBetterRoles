using AmongUs.Data;
using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime.Attributes;
using System.Collections;
using System.Reflection;
using TheBetterRoles.Helpers;
using UnityEngine;
using UnityEngine.Audio;

namespace TheBetterRoles.Managers;

internal class CustomClip
{
    internal AudioClip? Clip;
    internal string ClipName = string.Empty;
    internal float Volume = 1f;
}

internal class CustomSoundsManager : MonoBehaviour
{
    internal static CustomSoundsManager? Instance { get; private set; }

    private static readonly Dictionary<string, AudioClip?> soundEffects = [];
    private readonly Dictionary<AudioClip, AudioSource> allSources = [];

    internal static void CreateInstance()
    {
        if (Instance != null) return;

        GameObject CustomSoundManager = new("CustomSoundsManager");
        DontDestroyOnLoad(CustomSoundManager);
        CustomSoundManager.AddComponent<CustomSoundsManager>();
    }

    private void Awake()
    {
        Instance = this;

        soundEffects.Clear();
        Assembly assembly = Assembly.GetExecutingAssembly();
        string[] resourceNames = assembly.GetManifestResourceNames();
        foreach (string resourcePath in resourceNames)
        {
            if (resourcePath.Contains("TheBetterRoles.Resources.Sounds.") && resourcePath.EndsWith(".wav"))
            {
                string name = resourcePath.Replace("TheBetterRoles.Resources.Sounds.", "");
                this.StartCoroutine(Utils.LoadAudioClip(name, clip =>
                {
                    soundEffects[resourcePath] = clip;
                    Logger.Log($"Loaded AudioClip: {name}");
                }));
            }
        }
    }

    internal static AudioClip? GetClip(string name)
    {
        if (!name.Contains('.')) name = "TheBetterRoles.Resources.Sounds." + name + ".wav";
        return soundEffects.TryGetValue(name, out AudioClip? clip) ? clip : null;
    }

    internal AudioSource? PlaySound(AudioClip clip, bool loop, float volume = 1f, AudioMixerGroup? audioMixer = null)
    {
        volume *= DataManager.Settings.Audio.SfxVolume;

        if (clip == null)
        {
            Logger.Error("Missing audio clip");
            return null;
        }

        if (allSources.TryGetValue(clip, out AudioSource? audioSource))
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
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = audioMixer;
            audioSource.playOnAwake = false;
            audioSource.volume = volume;
            audioSource.loop = loop;
            audioSource.clip = clip;
            audioSource.Play();
            allSources[clip] = audioSource;
        }

        return audioSource;
    }

    internal void Play(string name, float volume = 1f, bool loop = false)
    {
        AudioClip? clipToPlay = GetClip(name);
        Stop(name);
        if (clipToPlay != null)
        {
            AudioSource? source = PlaySound(clipToPlay, loop, volume);
            if (source != null)
            {
                source.loop = loop;
            }
        }
    }

    internal void PlayDynamic(string name, Vector2 position, float range = 10f, float volume = 1f, bool loop = false)
    {
        AudioClip? clipToPlay = GetClip(name);
        if (clipToPlay != null)
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
                this.StartCoroutine(CoManageDynamicSound(soundObject, audioSource, position, range));
            }
        }
        else
        {
            Logger.Error($"Unable to play dynamic sound: {name}");
        }
    }

    [HideFromIl2Cpp]
    private IEnumerator CoManageDynamicSound(GameObject soundObject, AudioSource audioSource, Vector2 position, float range)
    {
        while (audioSource.isPlaying)
        {
            UpdateVolumeBasedOnDistance(audioSource, position, range);
            yield return null;
        }

        Destroy(soundObject);
    }

    private void UpdateVolumeBasedOnDistance(AudioSource audioSource, Vector2 sourcePosition, float range)
    {
        Vector2 playerPosition = PlayerControl.LocalPlayer.GetCustomPosition();
        float distance = Vector2.Distance(playerPosition, sourcePosition);

        float clampedDistance = Mathf.Clamp(distance, 0f, range);
        float volume = Mathf.Lerp(1f, 0f, Mathf.InverseLerp(range / 2f, range, clampedDistance));

        audioSource.volume = volume * DataManager.Settings.Audio.SfxVolume;
    }

    internal void StopSound(AudioClip? soundToStop)
    {
        if (soundToStop != null && allSources.TryGetValue(soundToStop, out AudioSource? source))
        {
            source.Stop();
            Destroy(source);
            allSources.Remove(soundToStop);
        }
    }

    internal void Stop(string name)
    {
        AudioClip? soundToStop = GetClip(name);
        if (soundToStop != null && allSources.TryGetValue(soundToStop, out AudioSource? source))
        {
            source.Stop();
            Destroy(source);
            allSources.Remove(soundToStop);
        }
    }

    internal void StopAll()
    {
        foreach (var source in allSources.Values)
        {
            source.Stop();
            Destroy(source);
        }
        allSources.Clear();
    }
}