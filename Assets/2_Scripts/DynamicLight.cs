using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

[RequireComponent(typeof(Light))]
public class DynamicLight : MonoBehaviour
{
    [Header("World Light")]
    [SerializeField] private bool respondToWorldLightLevel = true;
    [SerializeField, Min(0)] private float fadeDuration = 10f;
    
    [Header("Target Tracking")]
    [SerializeField] private bool trackTarget = false;
    [SerializeField] private bool returnToStartRotation = true;
    [SerializeField] private float returnToStartRotationDuration = 2f;
    [SerializeField, Min(0)] private float initialRotationSpeed = 10f; 
    [SerializeField, Range(0, 1)] private float initialSpeedDuration = 0.5f; 
    [SerializeField, Min(0)] private float rotationSpeed = 5f;
    [SerializeField, Min(0)] private float trackingDistance = 5f;
    [SerializeField] private Vector3 trackingOffset = Vector3.zero;
    [SerializeField] private Transform target;
    [SerializeField] private bool usePlayerAsTarget = false;
    
    private Light _light;
    private Tween _tween;
    private float _intensity;
    private float _defaultIntensity;
    private Quaternion _startRotation;
    private bool _isTracking = false;
    private float _trackingTimer = 0f; 

    private void Awake()
    {
        _light = GetComponent<Light>();
        _defaultIntensity = _light.intensity;
        _startRotation = transform.rotation;

        if (respondToWorldLightLevel)
        {
            _light.enabled = false;
            _light.intensity = 0;
        }
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
        
        if (trackTarget && usePlayerAsTarget)
        {
            target = PlayerStateMachine.Instance.transform;
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
    
    private void FixedUpdate()
    {
        if (!trackTarget || !target) return;
    
        float distanceToTarget = Vector3.Distance(transform.position + trackingOffset, target.position);
        bool shouldTrack = distanceToTarget <= trackingDistance;
    
        if (shouldTrack && _light.enabled)
        {
            if (!_isTracking)
            {
                // Just started tracking
                _isTracking = true;
                _trackingTimer = 0f; // Reset timer when we start tracking
            }
        
            // Calculate direction to look at
            Vector3 direction = target.position - transform.position;
            // Only rotate if we have a valid direction
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
            
                // Choose speed based on how long we've been tracking
                float currentSpeed;
                if (_trackingTimer < initialSpeedDuration)
                {
                    currentSpeed = initialRotationSpeed;
                }
                else
                {
                    currentSpeed = rotationSpeed;
                }
            
                // Smoothly rotate towards target
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, currentSpeed * Time.fixedDeltaTime);
            
                // Update tracking timer
                _trackingTimer += Time.fixedDeltaTime;
            }
        }
        else if (_isTracking && returnToStartRotation)
        {
            // Return to original rotation when target moves out of range
            _isTracking = false;
            _trackingTimer = 0f; // Reset timer when we stop tracking
            StartCoroutine(ReturnToStartRotation());
        }
    }
    
    private IEnumerator ReturnToStartRotation()
    {
        Quaternion currentRotation = transform.rotation;
        float elapsedTime = 0f;
        
        while (elapsedTime < returnToStartRotationDuration)
        {
            transform.rotation = Quaternion.Slerp(currentRotation, _startRotation, elapsedTime / returnToStartRotationDuration);
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


    #region Editor

    private void OnDrawGizmosSelected()
    {
        if (trackTarget)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + trackingOffset, trackingDistance);
        }
            
            
        if (trackTarget && target)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + trackingOffset, target.position);
        }
    }

    #endregion Editor
}