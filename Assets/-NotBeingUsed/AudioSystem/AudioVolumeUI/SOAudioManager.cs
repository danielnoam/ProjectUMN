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
    

    private void OnEnable()
    {
        if (audioMixer == null)
        {
            Debug.LogError("AudioMixer is not assigned!");
            return;
        }
    }
    

    public void SetVolume(VolumeType type, float volume)
    {
        if (audioMixer == null) return;
        
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

    // Use in a settings manager or gameplay manager because you cannot set it on enable in a scriptable object
    public void LoadAllVolumes()
    {
        SetVolume(VolumeType.MasterVolume, LoadVolume(VolumeType.MasterVolume));
        SetVolume(VolumeType.MusicVolume, LoadVolume(VolumeType.MusicVolume));
        SetVolume(VolumeType.SoundFXVolume, LoadVolume(VolumeType.SoundFXVolume));
    }
}
