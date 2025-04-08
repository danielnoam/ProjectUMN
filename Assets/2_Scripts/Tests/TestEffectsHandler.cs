using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using PrimeTween;
using UnityEngine.Serialization;

public class TestEffectsHandler : MonoBehaviour
{
    
    [Header("Settings")]
    [SerializeField, Min(0f)] private float introDurationEffectMultiplier = 0.7f;
    
    [Header("References")]
    [SerializeField] private Image fullscreenImage;
    private Volume _volume;
    private ChromaticAberration _chromaticAberration;
    private PaniniProjection _paniProjection;
    private Vignette _vignette;
    private TestManager _testManager;
    private Sequence _fadeSequence;
    
    private void Awake()
    {
        _testManager = GetComponent<TestManager>();
        _volume = FindFirstObjectByType<Volume>();
        if (_volume)
        {
            _volume.profile.TryGet(out _chromaticAberration);
            _volume.profile.TryGet(out _paniProjection);
            _volume.profile.TryGet(out _vignette);
        }
    }

    private void OnEnable()
    {
        _testManager?.onIntroSequenceStart.AddListener(OnIntroSequenceStart);
    }

    private void OnDisable()
    {
        _testManager?.onIntroSequenceStart.RemoveListener(OnIntroSequenceStart);
        _fadeSequence.Stop();
    }
    
    private void OnIntroSequenceStart()
    {
        FadeScreen(_testManager.IntroSequenceDuration * introDurationEffectMultiplier, true);
    }
    
    
    
    public void FadeScreen(float time, bool fadeIn)
    {
        if (_fadeSequence.isAlive) 
        {
            _fadeSequence.Stop();
        }
        
        
        _fadeSequence = Sequence.Create();

        _chromaticAberration.active = true;
        _chromaticAberration.intensity.overrideState = true;
        float startValue = fadeIn ? 1 : 0;
        float endValue = fadeIn ? 0 : 1;

        _fadeSequence = _fadeSequence
            .Group(Tween.Alpha(fullscreenImage, startValue, endValue, duration: time / 2f))
            .Group(Tween.Custom(startValue, endValue,  duration: time / 1.5f, onValueChange: val => _vignette.intensity.value = val))
            .Group(Tween.Custom(startValue, endValue,  duration: time, onValueChange: val =>_chromaticAberration.intensity.value = val))
            .Group(Tween.Custom(startValue, endValue,  duration: time, onValueChange: val => _paniProjection.distance.value = val))



            ;
    }
    

}
