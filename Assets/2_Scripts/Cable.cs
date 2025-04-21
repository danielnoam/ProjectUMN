using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;
using VInspector;

public enum CableState
{
    On,
    Off,
    Toggle
}

public class Cable : MonoBehaviour
{
    
    [Header("Emission Settings")]
    [SerializeField] private float breathingSpeed = 1.0f;
    [SerializeField] private float minEmissionMultiplier = 0.2f;
    [SerializeField] private float maxEmissionMultiplier = 1.0f;
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField, ReadOnly] private bool isOn;
    
    private Renderer _rend;
    private Material _material;
    private Color _emissionColor;
    private float _emissionIntensity;
    private Coroutine _breathingCoroutine;
    private Coroutine _transitionCoroutine;
    private static readonly int EmissionColor = Shader.PropertyToID("_Emission_Color");

    private void Awake()
    {
        _rend = GetComponent<Renderer>();
        
        if (_rend)
        {
            // Create a material instance to avoid changing the shared material
            _material = new Material(_rend.material);
            _rend.material = _material;
                
            // Enable emission on the material
            _material.EnableKeyword("_Emission");
            
            // Get the existing emission color and intensity from the material
            _emissionColor = _material.GetColor(EmissionColor);
            
            // Calculate the emission intensity from the brightest component
            _emissionIntensity = Mathf.Max(_emissionColor.r, _emissionColor.g, _emissionColor.b);
            
            // Normalize the color (so multiplying by _emissionIntensity gives the original color)
            if (_emissionIntensity > 0)
            {
                _emissionColor /= _emissionIntensity;
            }
            else
            {
                // Default to white if there's no emission
                _emissionColor = Color.white;
                _emissionIntensity = 1.0f;
            }
            
            // Set the initial emission state
            SetEmission(isOn, minEmissionMultiplier);
        }
    }
    
    [Button]
    public void TurnOn()
    {
        SetState(true);
    }
    
    [Button]
    public void TurnOff()
    {
        SetState(false);
    }
    
    [Button]
    public void Toggle()
    {
        SetState(!isOn);
    }


    public void SetState(bool active)
    {
        if (isOn == active) return;
        
        isOn = active;
        
        // Stop any existing transitions or breathing
        StopAllCoroutines();
        
        // Start transition coroutine
        _transitionCoroutine = StartCoroutine(TransitionEmission(active));
    }
    

    private IEnumerator TransitionEmission(bool turnOn)
    {
        float timeElapsed = 0;
        
        if (turnOn)
        {
            // Transition from off to on
            while (timeElapsed < transitionDuration)
            {
                float t = timeElapsed / transitionDuration;
                float intensity = Mathf.Lerp(0, _emissionIntensity * minEmissionMultiplier, t);
                _material.SetColor(EmissionColor, _emissionColor * intensity);
                
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            
            // Start breathing effect
            _breathingCoroutine = StartCoroutine(BreathingEffect());
        }
        else
        {
            // Get current emission intensity
            Color currentEmission = _material.GetColor(EmissionColor);
            float startIntensity = currentEmission.maxColorComponent;
            
            // Transition from on to off
            while (timeElapsed < transitionDuration)
            {
                float t = timeElapsed / transitionDuration;
                float intensity = Mathf.Lerp(startIntensity, 0, t);
                _material.SetColor(EmissionColor, _emissionColor * intensity);
                
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            
            // Ensure emission is fully off
            _material.SetColor(EmissionColor, Color.black);
        }
    }

    private IEnumerator BreathingEffect()
    {
        while (isOn)
        {
            // Use a sine wave to create a smooth breathing effect
            float breathValue = Mathf.Lerp(minEmissionMultiplier, maxEmissionMultiplier, 
                (Mathf.Sin(Time.time * breathingSpeed) + 1) * 0.5f);
            
            // Apply the breathing effect to the emission intensity
            _material.SetColor(EmissionColor, _emissionColor * (_emissionIntensity * breathValue));
            
            yield return null;
        }
    }
    

    private void SetEmission(bool active, float multiplier = 1.0f)
    {
        if (active)
        {
            _material.SetColor(EmissionColor, _emissionColor * (_emissionIntensity * multiplier));
        }
        else
        {
            _material.SetColor(EmissionColor, Color.black);
        }
    }
}