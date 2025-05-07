using UnityEngine;
using PrimeTween;
using VInspector;

public class TestLightingAnimator : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private Ease animationEase = Ease.Linear;
    
    [Header("References")]
    [SerializeField] private TestManager testManager;
    private Sequence _lightSequence;
    
    private void OnEnable()
    {
        testManager?.onTestStartUnloading.AddListener(OnTestStartUnloading);
        testManager?.onTestStartLoading.AddListener(OnTestStartLoading);
    }

    private void OnDisable()
    {
        testManager?.onTestStartUnloading.RemoveListener(OnTestStartUnloading);
        testManager?.onTestStartLoading.RemoveListener(OnTestStartLoading);
    }
    
    private void OnTestStartUnloading(SOTest test)
    {
        AnimateLightSettings(testManager.DefaultTest, false);
    }
    
    private void OnTestStartLoading(SOTest test)
    {
        AnimateLightSettings(testManager.CurrentTest, true);
    }
    
    
    private void ApplyLightSettings(SOTest test)
    {
        if (!test) return;
        
        var lightSettings = test.GetLightSettings();
        
        RenderSettings.defaultReflectionMode = lightSettings.reflectionMode;
        RenderSettings.fog = lightSettings.useFog;
        RenderSettings.fogMode = lightSettings.fogMode;

        
        RenderSettings.ambientIntensity = lightSettings.ambientIntensity;
        RenderSettings.fogColor = lightSettings.fogColor;
        switch (lightSettings.fogMode)
        {
            case FogMode.Exponential or FogMode.ExponentialSquared:
                RenderSettings.fogDensity = lightSettings.fogDensity;
                break;
            case FogMode.Linear:
                RenderSettings.fogStartDistance = lightSettings.fogStart;
                RenderSettings.fogEndDistance = lightSettings.fogEnd;
                break;
        }
    }

    private void AnimateLightSettings(SOTest test, bool loadIn)
    {
        if (!test) return;
        
        if (_lightSequence.isAlive)
        {
            _lightSequence.Stop();
        }
        
        var lightSettings = test.GetLightSettings();
        var duration = loadIn ? test.GetTimeToLoad() /2 : test.GetTimeToUnload();

        if (duration <= 1)
        {
            ApplyLightSettings(test);
            return;
        }
        
        RenderSettings.defaultReflectionMode = lightSettings.reflectionMode;
        RenderSettings.fog = lightSettings.useFog;
        RenderSettings.fogMode = lightSettings.fogMode;

        _lightSequence = Sequence.Create()

            .Group(Tween.Custom(
                onValueChange: intensity => RenderSettings.ambientIntensity = intensity,
                startValue: RenderSettings.ambientIntensity,
                endValue: lightSettings.ambientIntensity,
                duration: duration,
                ease: animationEase
            ))
            .Group(Tween.Custom(
                onValueChange: color => RenderSettings.fogColor = color,
                startValue: RenderSettings.fogColor,
                endValue: lightSettings.fogColor,
                duration: duration,
                ease: animationEase
            ))
            .Group(Tween.Custom(
                onValueChange: density => RenderSettings.fogDensity = density,
                startValue: RenderSettings.fogDensity,
                endValue: lightSettings.fogDensity,
                duration: duration,
                ease: animationEase
            ))
            .Group(Tween.Custom(
                onValueChange: start => RenderSettings.fogStartDistance = start,
                startValue: RenderSettings.fogStartDistance,
                endValue: lightSettings.fogStart,
                duration: duration,
                ease: animationEase
            ))
            .Group(Tween.Custom(
                onValueChange: end => RenderSettings.fogEndDistance = end,
                startValue: RenderSettings.fogEndDistance,
                endValue: lightSettings.fogEnd,
                duration: duration,
                ease: animationEase
            ))
            
            
            ;
    }

    [Button]
    private void ResetLightSettings()
    {
        if (!testManager || !testManager.DefaultTest) return;

    ApplyLightSettings(testManager.DefaultTest);
    }


}