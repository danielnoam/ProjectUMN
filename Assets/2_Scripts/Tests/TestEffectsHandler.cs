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
            
            _chromaticAberration.active = true;
            _chromaticAberration.intensity.overrideState = true;
            _paniProjection.active = true;
            _paniProjection.distance.overrideState = true;
        }
    }

    private void OnEnable()
    {
        _testManager?.onIntroSequenceStart.AddListener(OnIntroSequenceStart);
        _testManager?.onCreditsSequenceStart.AddListener(OnCreditsSequenceStart);
        _testManager?.onTestStartLoading.AddListener(OnTestStartLoading);
        _testManager?.onTestStartUnloading.AddListener(OnTestStartUnloading);
    }



    private void OnDisable()
    {
        _testManager?.onIntroSequenceStart.RemoveListener(OnIntroSequenceStart);
        _testManager?.onCreditsSequenceStart.RemoveListener(OnCreditsSequenceStart);
        _testManager?.onTestStartLoading.RemoveListener(OnTestStartLoading);
        _testManager?.onTestStartUnloading.RemoveListener(OnTestStartUnloading);
        _fadeSequence.Stop();
    }
    
    private void OnIntroSequenceStart()
    {
        if (_fadeSequence.isAlive) 
        {
            _fadeSequence.Stop();
        }
        
        
        
        float time = _testManager.IntroSequenceDuration * introDurationEffectMultiplier;
        float startValue = 1;
        float endValue = 0;

        _fadeSequence = Sequence.Create();
        _fadeSequence = _fadeSequence
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

        float time = _testManager.CreditsSequenceDuration * creditsDurationEffectMultiplier;

        _fadeSequence = Sequence.Create();
        _fadeSequence = _fadeSequence
                .Group(Tween.Custom(0, 0.3f,  duration: time, onValueChange: val => _vignette.intensity.value = val))
            ;
    }
    
    
    private void OnTestStartUnloading(SOTest test)
    {
        float time = test.GetTimeToUnload();
        float startValue = 0;
        float endValue = 1;

        _fadeSequence = Sequence.Create();
        _fadeSequence = _fadeSequence
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

        _fadeSequence = Sequence.Create();
        _fadeSequence = _fadeSequence
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

        _fadeSequence = Sequence.Create();
        _fadeSequence = _fadeSequence
                .Group(Tween.Alpha(fullscreenImage, startValue, endValue, duration: duration))
            ;

    }
    
}
