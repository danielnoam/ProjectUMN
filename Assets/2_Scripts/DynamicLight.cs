using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

[RequireComponent(typeof(Light))]
public class DynamicLight : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField, Min(0)] private float fadeDuration = 10f;
    [SerializeField] private bool respondToWorldLightLevel = true;
    
    [Header("Target Tracking")]
    [SerializeField] private bool trackTarget = false;
    [SerializeField] private Transform target;
    [SerializeField, Min(0)] private float trackingDistance = 5f;
    [SerializeField, Min(0)] private float rotationSpeed = 5f;
    
    private Light _light;
    private Tween _tween;
    private float _intensity;
    private float _defaultIntensity;
    private Quaternion _startRotation;
    private bool _isTracking = false;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _defaultIntensity = _light.intensity;
        _light.enabled = false;
        _light.intensity = 0;
        _startRotation = transform.rotation;
    }

    private void Start()
    {
        if (!TestManager.Instance && respondToWorldLightLevel)
        {
            FadeLight(TestManager.Instance.DefaultLightSettings, 0.5f);
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
        if (TestManager.Instance)
            TestManager.Instance.onTestLoaded.RemoveListener(OnTestLoaded);
        _tween.Stop();
    }
    
    private void OnTestLoaded(SOTest test)
    {
        if (!respondToWorldLightLevel) return;
        
        TestLightSettings lightSettings = test.GetLightSettings();
        bool fadeIn = lightSettings.ambientIntensity < 1;

        if (fadeIn)
        {
            FadeLight(lightSettings, fadeDuration);
        }
    }
    
    private void Update()
    {
        if (!trackTarget || !target) return;
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        bool shouldTrack = distanceToTarget <= trackingDistance;
        
        if (shouldTrack && _light.enabled)
        {
            _isTracking = true;
            // Calculate direction to look at
            Vector3 direction = target.position - transform.position;
            // Only rotate if we have a valid direction
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                // Smoothly rotate towards target
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else if (_isTracking)
        {
            // Return to original rotation when target moves out of range
            _isTracking = false;
            StartCoroutine(ReturnToStartRotation());
        }
    }
    
    private IEnumerator ReturnToStartRotation()
    {
        Quaternion currentRotation = transform.rotation;
        float elapsedTime = 0f;
        float returnDuration = 1f;
        
        while (elapsedTime < returnDuration)
        {
            transform.rotation = Quaternion.Slerp(currentRotation, _startRotation, elapsedTime / returnDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        transform.rotation = _startRotation;
    }
    
    

    private void FadeLight(TestLightSettings lightSettings, float duration)
    {
        if (_tween.isAlive) _tween.Stop();
        bool fadeIn = lightSettings.ambientIntensity < 1;
        
        _light.enabled = true;
        _tween = Tween.LightIntensity(_light, startValue: fadeIn ? 0 : _defaultIntensity, fadeIn ? _defaultIntensity : 0, duration);
        _tween.OnComplete(() => { 
            if (!fadeIn) _light.enabled = false;
            if (!_light.enabled) _isTracking = false;
        });
    }
}