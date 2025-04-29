using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Slider))]
public class VolumeSliderUI : MonoBehaviour
{
    [SerializeField] private VolumeType volumeType;
    [SerializeField] private SOAudioManager audioManager;
    [SerializeField] private TextMeshProUGUI sliderValue;
    [SerializeField] private SOAudioEvent previewSound;

    
    private AudioSource _audioSource;
    private Slider _slider;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _slider = GetComponent<Slider>();
    }

    private void Start()
    {
        SetUpSlider();
        SetSliderValue(_slider.value);
    }
    
    
    private void SetUpSlider()
    {
        if (!_slider) return;
        
        _slider.value = audioManager.LoadVolume(volumeType);
        _slider.onValueChanged.AddListener(SetVolume);
        _slider.onValueChanged.AddListener(SetSliderValue);
        _slider.onValueChanged.AddListener(_ => PlayPreviewSound());
        
    }
    
    
    
    private void SetSliderValue(float value)
    {
        if (!sliderValue) return;
        
        sliderValue.text = Mathf.Round(value * 100) + "%";
    }
    
    private void SetVolume(float volume)
    {
        if (!audioManager) return;
        
        audioManager.SetVolume(volumeType, volume);
        audioManager.SaveVolume(volumeType, volume);
        
    }
    
    private void PlayPreviewSound()
    {
        if (!previewSound || !_audioSource) return;

        previewSound.Play(_audioSource);
    }
    
}
