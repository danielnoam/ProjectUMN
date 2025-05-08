using System;
using UnityEngine;
using UnityEngine.Audio;



public enum VolumeType
{
    MasterVolume,
    MusicVolume,
    SoundFXVolume
}

[CreateAssetMenu(fileName = "SOAudioManager", menuName = "SO Audio/Audio Manager")] 
public class SOAudioManager : ScriptableObject
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    
    [Header("Default Volume Levels")]
    [SerializeField] private float defaultMasterVolume = 0.75f;
    [SerializeField] private float defaultMusicVolume = 0.6f;
    [SerializeField] private float defaultSoundFXVolume = 1f;
    
    
    public float DefaultMasterVolume => defaultMasterVolume;
    public float DefaultMusicVolume => defaultMusicVolume;
    public float DefaultSoundFXVolume => defaultSoundFXVolume;
    
    public void SetVolume(VolumeType type, float volume)
    {
        if (!audioMixer) return;
        
        // Convert slider value (usually 0 to 1) to decibels
        float dB = Mathf.Log10(volume) * 20;
        
        // Protect against - infinity when volume is 0
        if (volume == 0) { dB = -80f; }
        
        switch (type)
        {
            case VolumeType.MasterVolume:
                audioMixer.SetFloat("masterVolume", dB);

                break;
            case VolumeType.MusicVolume:
                audioMixer.SetFloat("musicVolume", dB);

                break;
            case VolumeType.SoundFXVolume:
                audioMixer.SetFloat("soundFXVolume", dB);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    
    
    public void SaveVolume(VolumeType type, float volume)
    {
        PlayerPrefs.SetFloat(type.ToString(), volume);
        PlayerPrefs.Save();
    }
    
    public float LoadVolume(VolumeType type)
    {
        return PlayerPrefs.GetFloat(type.ToString(), 1f);
    }
    
    private float LoadMasterVolume()
    {
        if (!PlayerPrefs.HasKey(nameof(VolumeType.MasterVolume)))
        {
            PlayerPrefs.SetFloat(nameof(VolumeType.MasterVolume), defaultMasterVolume);
            PlayerPrefs.Save();
        }
        return PlayerPrefs.GetFloat(nameof(VolumeType.MasterVolume), defaultMasterVolume);
    }
    
    private float LoadMusicVolume()
    {
        if (!PlayerPrefs.HasKey(nameof(VolumeType.MusicVolume)))
        {
            PlayerPrefs.SetFloat(nameof(VolumeType.MusicVolume), defaultMusicVolume);
            PlayerPrefs.Save();
        }
        
        return PlayerPrefs.GetFloat(nameof(VolumeType.MusicVolume),defaultMusicVolume);
    }
    
    private float LoadSoundFXVolume()
    {
        if (!PlayerPrefs.HasKey(nameof(VolumeType.SoundFXVolume)))
        {
            PlayerPrefs.SetFloat(nameof(VolumeType.SoundFXVolume), defaultSoundFXVolume);
            PlayerPrefs.Save();
        }
        return PlayerPrefs.GetFloat(nameof(VolumeType.SoundFXVolume), defaultSoundFXVolume);
    }

    // Use in a settings manager or gameplay manager because you cannot set it on enabling in a scriptable object
    public void LoadAllVolumes()
    {
        SetVolume(VolumeType.MasterVolume, LoadMasterVolume());
        SetVolume(VolumeType.MusicVolume, LoadMusicVolume());
        SetVolume(VolumeType.SoundFXVolume, LoadSoundFXVolume());
    }
    
    // Use in a settings manager or gameplay manager because you cannot set it on enabling in a scriptable object
    public void ResetAllVolumes()
    {
        SetVolume(VolumeType.MasterVolume, defaultMasterVolume);
        SetVolume(VolumeType.MusicVolume, defaultMusicVolume);
        SetVolume(VolumeType.SoundFXVolume, defaultSoundFXVolume);
    }
}
