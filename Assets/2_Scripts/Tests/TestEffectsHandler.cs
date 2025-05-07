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
    [SerializeField, Min(0f)] private float introDurationEffectMultiplier = 1f;
    [SerializeField, Min(0f)] private float creditsDurationEffectMultiplier = 1f;
    
    [Header("References")]
    [SerializeField] private TestManager testManager;
    [SerializeField] private Image fullscreenImage;
    [SerializeField] private Volume volume;
    private ChromaticAberration _chromaticAberration;
    private PaniniProjection _paniProjection;
    private Vignette _vignette;
    
    private Sequence _fadeSequence;
    
    private void Awake()
    {
        if (!volume) volume = FindFirstObjectByType<Volume>();
        if (volume)
        {
            volume.profile.TryGet(out _chromaticAberration);
            volume.profile.TryGet(out _paniProjection);
            volume.profile.TryGet(out _vignette);
            
            _chromaticAberration.active = true;
            _chromaticAberration.intensity.overrideState = true;
            _paniProjection.active = true;
            _paniProjection.distance.overrideState = true;
        }
    }

    private void OnEnable()
    {
        testManager?.onIntroSequenceStart.AddListener(OnIntroSequenceStart);
        testManager?.onCreditsSequenceStart.AddListener(OnCreditsSequenceStart);
        testManager?.onTestStartLoading.AddListener(OnTestStartLoading);
        testManager?.onTestStartUnloading.AddListener(OnTestStartUnloading);
    }



    private void OnDisable()
    {
        testManager?.onIntroSequenceStart.RemoveListener(OnIntroSequenceStart);
        testManager?.onCreditsSequenceStart.RemoveListener(OnCreditsSequenceStart);
        testManager?.onTestStartLoading.RemoveListener(OnTestStartLoading);
        testManager?.onTestStartUnloading.RemoveListener(OnTestStartUnloading);
        _fadeSequence.Stop();
    }
    
    private void OnIntroSequenceStart()
    {
        if (_fadeSequence.isAlive) 
        {
            _fadeSequence.Stop();
        }
        
        
        float time = testManager.IntroSequenceDuration * introDurationEffectMultiplier;
        float startValue = 1;
        float endValue = 0;

        fullscreenImage.color = Color.black;
        _vignette.intensity.value = 1;
        _chromaticAberration.intensity.value = 1;
        _paniProjection.distance.value = 1;
        
        _fadeSequence = Sequence.Create()
                .ChainDelay(1f)
                .Group(Tween.Alpha(fullscreenImage, startValue, endValue, duration: time / 3f))
                .Group(Tween.Custom(startValue, endValue,  duration: time / 1.5f, onValueChange: val => _vignette.intensity.value = val))
                .Group(Tween.Custom(startValue, endValue,  duration: time * 2, onValueChange: val =>_chromaticAberration.intensity.value = val))
                .Group(Tween.Custom(startValue, endValue,  duration: time, onValueChange: val => _paniProjection.distance.value = val))
            ;
    }
    
    private void OnCreditsSequenceStart()
    {
        
        if (_fadeSequence.isAlive) 
        {
            _fadeSequence.Stop();
        }

        float time = testManager.CreditsSequenceDuration * creditsDurationEffectMultiplier;
        
        _fadeSequence = Sequence.Create()
                .Group(Tween.Custom(0, 0.3f,  duration: time, onValueChange: val => _vignette.intensity.value = val))
                .Group(Tween.Custom(_chromaticAberration.intensity.value, 0,  duration: time, onValueChange: val =>_chromaticAberration.intensity.value = val))
                .Group(Tween.Custom(_paniProjection.distance.value, 0,  duration: time, onValueChange: val => _paniProjection.distance.value = val))
            ;
    }
    
    
    private void OnTestStartUnloading(SOTest test)
    {
        float time = test.GetTimeToUnload();
        float startValue = 0;
        float endValue = 1;
        
        _fadeSequence = Sequence.Create()
                .Group(Tween.Custom(startValue, 0.4f,  duration: time, onValueChange: val => _vignette.intensity.value = val))
                .Group(Tween.Custom(startValue, endValue,  duration: time, onValueChange: val =>_chromaticAberration.intensity.value = val))
                .Group(Tween.Custom(startValue, endValue,  duration: time, onValueChange: val => _paniProjection.distance.value = val))
            ;
    }

    private void OnTestStartLoading(SOTest test)
    {

        float time = test.GetTimeToLoad();
        float startValue = 1;
        float endValue = 0;
        
        _fadeSequence = Sequence.Create()
                .Group(Tween.Custom(0.4f, endValue,  duration: time, onValueChange: val => _vignette.intensity.value = val))
                .Group(Tween.Custom(startValue, endValue,  duration: time, onValueChange: val =>_chromaticAberration.intensity.value = val))
                .Group(Tween.Custom(startValue, endValue,  duration: time, onValueChange: val => _paniProjection.distance.value = val))
            ;

    }

    public void FadeScreen(bool fadeIn, float duration)
    {
        if (_fadeSequence.isAlive) 
        {
            _fadeSequence.Stop();
        }
        
        float startValue = fadeIn ? 0 : 1;
        float endValue = fadeIn ? 1 : 0;
        
        _vignette.intensity.value = 0;
        _chromaticAberration.intensity.value = 0;
        _paniProjection.distance.value = 0;
        
        _fadeSequence = Sequence.Create()
                .Group(Tween.Alpha(fullscreenImage, startValue, endValue, duration: duration))
            ;

    }
    
    
    public void ForceStopAllEffects()
    {
        if (_fadeSequence.isAlive) 
        {
            _fadeSequence.Stop();
        }
        
        fullscreenImage.color = Color.clear;
        _vignette.intensity.value = 0;
        _chromaticAberration.intensity.value = 0;
        _paniProjection.distance.value = 0;
    }
    
}
