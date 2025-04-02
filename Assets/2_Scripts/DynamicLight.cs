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
        _light.intensity = 0;
    }

    private void Start()
    {
        if (!TestManager.Instance) return;
        TestManager.Instance.onTestLoaded.AddListener(FadeLight);
        
        
        if (TestManager.Instance.CurrentTest) FadeLight(TestManager.Instance.CurrentTest);
    }
    
    private void OnEnable()
    {
        if (!TestManager.Instance) return;
        TestManager.Instance.onTestLoaded.AddListener(FadeLight);
    }

    private void OnDisable()
    {
        TestManager.Instance.onTestLoaded.RemoveListener(FadeLight);
    }
    

    private void FadeLight(SOTest test)
    {
        if (_tween.isAlive) _tween.Stop();
        
        
        TestLightSettings lightSettings = test.GetLightSettings();
        bool fadeIn = lightSettings.ambientIntensity < 1;
        
        _tween = Tween.LightIntensity(_light, fadeIn ? _defaultIntensity : 0, fadeDuration);
    }
}
