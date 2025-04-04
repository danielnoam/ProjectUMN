using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

[RequireComponent(typeof(Light))]
public class DynamicLight : MonoBehaviour
{

    [Header("Settings")] 
    [SerializeField, Min(0)] private  float fadeDuration = 7f;
    private Light _light;
    private Tween _tween;
    private float _intensity;
    private float _defaultIntensity;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _defaultIntensity = _light.intensity;
        _light.enabled = false;
        _light.intensity = 0;
    }

    private void Start()
    {
        if (!TestManager.Instance)
        {
            FadeLight(TestManager.Instance.DefaultLightSettings);
        }
        else
        {
            TestManager.Instance.onTestLoaded.AddListener(OnTestLoaded);
        }
        
    }
    
    private void OnEnable()
    {
        if (!TestManager.Instance) return;
        TestManager.Instance.onTestLoaded.AddListener(OnTestLoaded);
    }

    private void OnDisable()
    {
        TestManager.Instance.onTestLoaded.RemoveListener(OnTestLoaded);
        _tween.Stop();
    }

    private void OnTestLoaded(SOTest test)
    {
        FadeLight(test.GetLightSettings());
    }

    private void FadeLight(TestLightSettings lightSettings)
    {
        if (_tween.isAlive) _tween.Stop();
        bool fadeIn = lightSettings.ambientIntensity < 1;
        
        
        _light.enabled = true;
        _tween = Tween.LightIntensity(_light,startValue: fadeIn ? 0 : _defaultIntensity, fadeIn ? _defaultIntensity : 0, fadeDuration);
        _tween.OnComplete(() => { if (!fadeIn) _light.enabled = false; });
    }
}
