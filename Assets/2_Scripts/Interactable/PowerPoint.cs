using System;
using UnityEngine;
using VInspector;
using PrimeTween;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

[SelectionBase]
[RequireComponent(typeof(AudioSource))]
public class PowerPoint : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField, Min(0), Tooltip("The number of power sources needed to fully activate this power point")]
    private float powerSourcesNeeded = 1;
    
    [SerializeField, Range(0.1f, 5f), Tooltip("Controls the power response curve. Values below 1 make differences more noticeable at low power, values above 1 create steeper changes at high power")]
    private float powerCurve = 2f;
    [EndIf]
    
    [Header("Rotation")]
    [SerializeField, Tooltip("The transform that will rotate when power is applied")]
    private Transform rotationPivot;
    
    [SerializeField, Tooltip("Maximum rotation speed in degrees per second at full power")]
    private float rotationSpeed = 500f;
    
    [SerializeField, Tooltip("How quickly the rotation speed increases when more power is applied (degrees per second)")]
    private float rotationAcceleration = 100f;
    
    [SerializeField, Tooltip("How quickly the rotation speed decreases when power is reduced (degrees per second)")]
    private float rotationDeceleration = 75f;
    
    [SerializeField, Tooltip("Direction of rotation (normalized in code)")]
    private Vector3 rotationDirection = Vector3.up;
    
    [Header("Light")]
    [SerializeField, Tooltip("Reference to the light component that will change intensity with power")]
    private Light pointLight;
    
    [SerializeField, Tooltip("Maximum light intensity at full power")]
    private float maxLightIntensity = 5f;
    
    [SerializeField, Tooltip("How quickly the light intensity increases when more power is applied")]
    private float lightAcceleration = 1f;
    
    [SerializeField, Tooltip("How quickly the light intensity decreases when power is reduced")]
    private float lightDeceleration = 1f;
    
    [Header("Material")]
    [SerializeField, Tooltip("Reference to the renderer component whose material will have emission")]
    private Renderer materialRenderer;
    
    [SerializeField, Tooltip("Maximum emission intensity at full power")]
    private float maxEmissionIntensity = 5f;
    
    [SerializeField, Tooltip("Color of the emission effect")]
    private Color emissionColor = Color.cyan;
    
    [SerializeField, Tooltip("How quickly the emission intensity increases when more power is applied")]
    private float emissionAcceleration = 1f;
    
    [SerializeField, Tooltip("How quickly the emission intensity decreases when power is reduced")]
    private float emissionDeceleration = 1f;
    
    [Header("Events")]
    [SerializeField, Tooltip("Event triggered when the power point becomes fully activated")]
    private UnityEvent onActivated;           
    
    [SerializeField, Tooltip("Event triggered when the power point becomes deactivated")]
    private UnityEvent onDeactivated;   
    
    [Space(10)]
    [SerializeField, ReadOnly, Tooltip("Is the power point currently activated (has reached or exceeded required power sources)")]
    private bool isOn;
    
    [SerializeField, ReadOnly, Tooltip("Current number of power sources connected")]
    private float powerSources;
    
    [SerializeField, ReadOnly, Tooltip("Current power ratio after applying the power curve")]
    private float powerRatio;
    private readonly HashSet<Object> _powerSources = new HashSet<Object>();
    private float _currentRotationSpeed;
    private float _currentLightIntensity;
    private float _currentEmissionIntensity;
    private AudioSource _audioSource;
    
    private Material _pivotMaterial;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int EmissionEnabled = Shader.PropertyToID("_EmissionEnabled");

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        
        // Get the material from the rotation pivot
        if (materialRenderer)
        {
            Renderer renderer = materialRenderer;
            if (renderer)
            {
                // Create a material instance to avoid changing the shared material
                _pivotMaterial = new Material(renderer.material);
                renderer.material = _pivotMaterial;
                
                // Enable emission on the material
                _pivotMaterial.EnableKeyword("_EMISSION");
            }
        }
    }

    private void Update()
    {
        CheckState();
        HandleFeedback();
    }

    private void CheckState()
    {
        powerSources = _powerSources.Count;
        
        if (_powerSources.Count >= powerSourcesNeeded && !isOn && _currentRotationSpeed >= rotationSpeed/3)
        {
            isOn = true;
            onActivated?.Invoke();
        } 
        else if (_powerSources.Count < powerSourcesNeeded && isOn && _currentRotationSpeed <= rotationSpeed/2)
        {
            isOn = false;
            onDeactivated?.Invoke();
        } 
        
        // Calculate power ratio with an exponential curve
        float rawRatio = Mathf.Clamp01(powerSources / powerSourcesNeeded);
        powerRatio = Mathf.Pow(rawRatio, powerCurve);
    }
    
    private void HandleFeedback()
    {
        // Material Emission
        if (_pivotMaterial)
        {
            // Calculate target emission intensity based on power ratio
            float targetEmissionIntensity = maxEmissionIntensity * powerRatio;
            
            // Apply acceleration or deceleration based on whether we're increasing or decreasing
            if (targetEmissionIntensity > _currentEmissionIntensity)
            {
                // Increasing intensity - use acceleration
                _currentEmissionIntensity = Mathf.Min(_currentEmissionIntensity + emissionAcceleration * Time.deltaTime, targetEmissionIntensity);
            }
            else if (targetEmissionIntensity < _currentEmissionIntensity)
            {
                // Decreasing intensity - use deceleration
                _currentEmissionIntensity = Mathf.Max(_currentEmissionIntensity - emissionDeceleration * Time.deltaTime, targetEmissionIntensity);
            }
            
            // Apply the smoothed emission intensity
            _pivotMaterial.SetColor(EmissionColor, emissionColor * _currentEmissionIntensity);
        }
        
        // Light
        if (pointLight)
        {
            // Calculate target light intensity based on power ratio
            float targetLightIntensity = maxLightIntensity * powerRatio;
            
            // Apply acceleration or deceleration based on whether we're increasing or decreasing
            if (targetLightIntensity > _currentLightIntensity)
            {
                // Increasing intensity - use acceleration
                _currentLightIntensity = Mathf.Min(_currentLightIntensity + lightAcceleration * Time.deltaTime, targetLightIntensity);
            }
            else if (targetLightIntensity < _currentLightIntensity)
            {
                // Decreasing intensity - use deceleration
                _currentLightIntensity = Mathf.Max(_currentLightIntensity - lightDeceleration * Time.deltaTime, targetLightIntensity);
            }
            
            // Apply the smoothed intensity
            pointLight.intensity = _currentLightIntensity;
        }
     
        // Audio
        if (isOn)
        {
            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }
        else
        {
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
        }
        
        // Rotation
        if (rotationPivot)
        {
            // Calculate target rotation speed based on power ratio
            float targetRotationSpeed = rotationSpeed * powerRatio;
        
            // Apply acceleration or deceleration based on whether we're speeding up or slowing down
            if (targetRotationSpeed > _currentRotationSpeed)
            {
                // Speeding up - use acceleration
                _currentRotationSpeed = Mathf.Min(_currentRotationSpeed + rotationAcceleration * Time.deltaTime, targetRotationSpeed);
            }
            else if (targetRotationSpeed < _currentRotationSpeed)
            {
                // Slowing down - use deceleration
                _currentRotationSpeed = Mathf.Max(_currentRotationSpeed - rotationDeceleration * Time.deltaTime, targetRotationSpeed);
            }
        
            // Apply rotation using the smoothed speed
            rotationPivot.Rotate(rotationDirection * (_currentRotationSpeed * Time.deltaTime));
        }
    }
    
    public void AddPowerSource(Object source)
    {
        _powerSources.Add(source);
    }
    
    public void RemovePowerSource(Object source)
    {
        _powerSources.Remove(source);
    }
}