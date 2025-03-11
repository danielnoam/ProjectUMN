using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class VolumeSliderUI : MonoBehaviour
{
    [SerializeField] private VolumeType volumeType;
    [SerializeField] private SOAudioManager audioManager;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI sliderLabel;
    [SerializeField] private TextMeshProUGUI sliderValue;
    [SerializeField] private SOAudioEvent previewSound;

    private void Start()
    {
        SetUpSlider();
        SetSliderName();
        SetSliderValue(slider.value);
    }
    
    
    private void SetUpSlider()
    {
        if (slider == null) return;
        
        slider.value = audioManager.LoadVolume(volumeType);
        slider.onValueChanged.AddListener(SetVolume);
        slider.onValueChanged.AddListener(SetSliderValue);
        slider.onValueChanged.AddListener(_ => PlayPreviewSound());
        
    }
    
    
    private void SetSliderName()
    {
        if (sliderLabel == null) return;
        
        if (volumeType.ToString().Contains("Volume"))
        {
            sliderLabel.text = volumeType.ToString().Insert(volumeType.ToString().IndexOf("Volume", StringComparison.Ordinal), " ");
        }


    }
    
    private void SetSliderValue(float value)
    {
        if (sliderValue == null) return;
        
        sliderValue.text = Mathf.Round(value * 100) + "%";
    }
    
    private void SetVolume(float volume)
    {
        if (audioManager == null) return;
        
        audioManager.SetVolume(volumeType, volume);
        audioManager.SaveVolume(volumeType, volume);
        
    }
    
    private void PlayPreviewSound()
    {
        if (previewSound == null || audioSource == null) return;

        previewSound.Play(audioSource);
    }
    
    

#if UNITY_EDITOR
    private void OnValidate()
    {
        SetSliderName();
    }
#endif
}
