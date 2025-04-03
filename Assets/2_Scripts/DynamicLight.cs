using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

[RequireComponent(typeof(Light))]
public class DynamicLight : MonoBehaviour
{

    [Header("Settings")] 
    [SerializeField, Min(0)] private  float fadeDuration = 10f;
    private Light _light;
    private Tween _tween;
    private float _intensity;
    private float _defaultIntensity;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _defaultIntensity = _light.intensity;
    }

    private void Start()
    {
        if (!TestManager.Instance) return;
        TestManager.Instance.onTestLoaded.AddListener(OnTestLoaded);
        FadeLight(TestManager.Instance.DefaultLightSettings);
    }
    
    private void OnEnable()
    {
        if (!TestManager.Instance) return;
        TestManager.Instance.onTestLoaded.AddListener(OnTestLoaded);
    }

    private void OnDisable()
    {
        TestManager.Instance.onTestLoaded.RemoveListener(OnTestLoaded);
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
        _tween = Tween.LightIntensity(_light, fadeIn ? _defaultIntensity : 0, fadeDuration);
        _tween.OnComplete(() => { if (!fadeIn) _light.enabled = false; });
    }
}
