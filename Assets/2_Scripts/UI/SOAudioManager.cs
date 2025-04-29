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
    [SerializeField] private AudioMixer audioMixer;
    
    
    
    public void SetVolume(VolumeType type, float volume)
    {
        if (!audioMixer) return;
        
        // Convert slider value (usually 0 to 1) to decibels
        float dB = Mathf.Log10(volume) * 20;
        
        // Protect against -infinity when volume is 0
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
        return PlayerPrefs.GetFloat(nameof(VolumeType.MasterVolume), 0.75f);
    }
    
    private float LoadMusicVolume()
    {
        return PlayerPrefs.GetFloat(nameof(VolumeType.MusicVolume), 0.4f);
    }
    
    private float LoadSoundFXVolume()
    {
        return PlayerPrefs.GetFloat(nameof(VolumeType.SoundFXVolume), 1f);
    }

    // Use in a settings manager or gameplay manager because you cannot set it on enable in a scriptable object
    public void LoadAllVolumes()
    {
        SetVolume(VolumeType.MasterVolume, LoadMasterVolume());
        SetVolume(VolumeType.MusicVolume, LoadMusicVolume());
        SetVolume(VolumeType.SoundFXVolume, LoadSoundFXVolume());
    }
}
