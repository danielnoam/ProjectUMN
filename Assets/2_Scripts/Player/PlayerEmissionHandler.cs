using System;
using System.Collections;
using UnityEngine;
using VInspector;

public class PlayerEmissionHandler : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool enableEmission = true;
    [SerializeField] private float breathingSpeed = 1.0f;
    [SerializeField] private float minEmissionMultiplier = 0.2f;
    [SerializeField] private float maxEmissionMultiplier = 1.0f;
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField, ColorUsage(false, true)] private Color damageColor = Color.red;
    [SerializeField] private float deathIntensityMultiplier = 2.0f;
    [SerializeField, ReadOnly] private bool isOn;
    
    [Header("References")]
    [SerializeField] private PlayerStateMachine player;
    [SerializeField] private Renderer rend;

    private static readonly int EmissionColor = Shader.PropertyToID("_Emission_Color");
    private static readonly int Emission = Shader.PropertyToID("_Emission");
    private Material _material;
    private Color _defaultEmissionColor;
    private float _emissionIntensity;
    private Coroutine _activeCoroutine;
    private enum EmissionState { Off, Normal, Death }
    private EmissionState _currentState = EmissionState.Off;
    
    private void Awake()
    {
        if (!enableEmission || !rend || !rend.material) 
        {
            Debug.LogWarning($"Emission disabled or no renderer/material found on {gameObject.name}");
            return;
        }
        
        InitializeMaterial();
    }
    

    private void OnEnable()
    {
        player?.onPlayerDeath.AddListener(OnPlayerDeath);
        player?.onPlayerSpawnedFromCheckpoint.AddListener(OnPlayerSpawnedFromCheckpoint);
    }
    
    private void OnDisable()
    {
        player?.onPlayerDeath.RemoveListener(OnPlayerDeath);
        player?.onPlayerSpawnedFromCheckpoint.RemoveListener(OnPlayerSpawnedFromCheckpoint);
        
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }
        
    }
    
    private void OnPlayerDeath()
    {
        TransitionToState(EmissionState.Death);
    }
    
    private void OnPlayerSpawnedFromCheckpoint(ISpawnPoint spawnPoint)
    {
        TransitionToState(EmissionState.Normal);
    }
    
    [Button]
    private void Toggle()
    {
        TransitionToState(isOn ? EmissionState.Off : EmissionState.Normal);
    }
    
    private void TransitionToState(EmissionState newState)
    {
        if (!rend || !_material) return;
        
        // Stop any active coroutine
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }
        
        // Update isOn based on the new state
        isOn = newState != EmissionState.Off;
        
        // Start transition to the new state
        _activeCoroutine = StartCoroutine(EmissionTransition(_currentState, newState));
        
        // Update current state
        _currentState = newState;
    }
    
    private IEnumerator EmissionTransition(EmissionState fromState, EmissionState toState)
    {
        float timeElapsed = 0;
        
        // Get the current color and target color based on states
        Color startColor = GetColorForState(fromState);
        Color targetColor = GetColorForState(toState);
        
        // Get appropriate intensities
        float startIntensity = GetIntensityForState(fromState);
        float targetIntensity = GetIntensityForState(toState);
        
        // Perform the transition
        while (timeElapsed < transitionDuration)
        {
            float t = timeElapsed / transitionDuration;
            
            // Lerp between colors and intensities
            Color lerpedColor = Color.Lerp(startColor, targetColor, t);
            float intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            
            // Apply the transitioned color
            _material.SetColor(EmissionColor, lerpedColor * intensity);
            
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final state is applied exactly
        _material.SetColor(EmissionColor, targetColor * targetIntensity);
        
        // If transitioning to normal state, start breathing effect
        if (toState == EmissionState.Normal)
        {
            _activeCoroutine = StartCoroutine(BreathingEffect());
        }
    }
    
    private Color GetColorForState(EmissionState state)
    {
        switch (state)
        {
            case EmissionState.Off:
                return Color.black;
            case EmissionState.Normal:
                return _defaultEmissionColor;
            case EmissionState.Death:
                return damageColor;
            default:
                return _defaultEmissionColor;
        }
    }
    
    private float GetIntensityForState(EmissionState state)
    {
        switch (state)
        {
            case EmissionState.Off:
                return 0f;
            case EmissionState.Normal:
                return _emissionIntensity * minEmissionMultiplier;
            case EmissionState.Death:
                return _emissionIntensity * deathIntensityMultiplier;
            default:
                return _emissionIntensity;
        }
    }
    
    private void UpdateEmissionState(EmissionState state)
    {
        if (!_material) return;
        
        Color color = GetColorForState(state);
        float intensity = GetIntensityForState(state);
        
        _material.SetColor(EmissionColor, color * intensity);
        _currentState = state;
        
        // Start breathing if in normal state
        if (state == EmissionState.Normal && _activeCoroutine == null)
        {
            _activeCoroutine = StartCoroutine(BreathingEffect());
        }
    }
    
    private IEnumerator BreathingEffect()
    {
        while (_currentState == EmissionState.Normal && isOn)
        {
            // Calculate breathing value using sine wave
            float breathValue = Mathf.Lerp(minEmissionMultiplier, maxEmissionMultiplier, 
                (Mathf.Sin(Time.time * breathingSpeed) + 1) * 0.5f);
            
            // Apply breathing effect to emission
            _material.SetColor(EmissionColor, _defaultEmissionColor * (_emissionIntensity * breathValue));
            
            yield return null;
        }
    }
    
    
    private void InitializeMaterial()
    {
        // Create a material instance to avoid changing the shared material
        _material = new Material(rend.material);
        rend.material = _material;
        
        // Check if the emission properties exist in the material
        if (!_material.HasProperty(EmissionColor) || !_material.HasProperty(Emission))
        {
            Debug.LogWarning($"Material on {gameObject.name} does not have required emission properties");
            return;
        }
        
        // Enable emission on the material
        _material.EnableKeyword("_Emission");
        
        // Get the existing emission color from the material
        _defaultEmissionColor = _material.GetColor(EmissionColor);
        
        // Calculate the emission intensity from the brightest component
        _emissionIntensity = Mathf.Max(_defaultEmissionColor.r, _defaultEmissionColor.g, _defaultEmissionColor.b);
        
        // Normalize the color if it has intensity
        if (_emissionIntensity > 0)
        {
            _defaultEmissionColor /= _emissionIntensity;
        }
        else
        {
            // Default to white if there's no emission
            _defaultEmissionColor = Color.white;
            _emissionIntensity = 1.0f;
        }
        
        // Initialize the material with the appropriate state
        UpdateEmissionState(isOn ? EmissionState.Normal : EmissionState.Off);
    }
}
