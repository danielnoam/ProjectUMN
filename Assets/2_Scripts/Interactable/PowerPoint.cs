using UnityEngine;
using VInspector;
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
    
    [SerializeField, Tooltip("If enabled, the power point will remain active after first activation, even if power sources are removed")]
    private bool stayActiveAfterFirstActivation = false;
    
    [Header("Feedback")] 
    [SerializeField] private CableState cableStateOnActivate = CableState.Toggle;
    [SerializeField] private CableState cableStateOnDeactivate = CableState.Toggle;
    [SerializeField] private Cable[] connectedCables;
    [SerializeField] private SOAudioEvent sfxLoop;
    
    [Foldout("Rotation")]
    [SerializeField, Tooltip("The transform that will rotate when power is applied")]
    private Transform rotationPivot;
    
    [SerializeField, Tooltip("Maximum rotation speed in degrees per second at full power")]
    private float rotationSpeed = 500f;
    
    [SerializeField, Tooltip("How quickly the rotation speed increases when more power is applied (degrees per second)")]
    private float rotationAcceleration = 100f;
    
    [SerializeField, Tooltip("How quickly the rotation speed decreases when power is reduced (degrees per second)")]
    private float rotationDeceleration = 150f;
    
    [SerializeField, Tooltip("Direction of rotation (normalized in code)")]
    private Vector3 rotationDirection = Vector3.up;
    [EndFoldout]
    
    [Foldout("Light")]
    [SerializeField, Tooltip("Reference to the light component that will change intensity with power")]
    private Light pointLight;
    
    [SerializeField, Tooltip("Minimum light intensity when no power is applied")]
    private float minLightIntensity = 0f;
    
    [SerializeField, Tooltip("Maximum light intensity at full power")]
    private float maxLightIntensity = 5f;
    
    [SerializeField, Tooltip("How quickly the light intensity increases when more power is applied")]
    private float lightAcceleration = 1f;
    
    [SerializeField, Tooltip("How quickly the light intensity decreases when power is reduced")]
    private float lightDeceleration = 1.5f;
    [EndFoldout]
    
    [Foldout("Material")]
    [SerializeField, Tooltip("Reference to the renderer component whose material will have emission")]
    private Renderer materialRenderer;
    
    [SerializeField, Tooltip("Maximum emission intensity at full power")]
    private float maxEmissionIntensity = 5f;
    
    [SerializeField, Tooltip("Color of the emission effect")]
    private Color emissionColor = Color.cyan;
    
    [SerializeField, Tooltip("How quickly the emission intensity increases when more power is applied")]
    private float emissionAcceleration = 1f;
    
    [SerializeField, Tooltip("How quickly the emission intensity decreases when power is reduced")]
    private float emissionDeceleration = 1.5f;
    [EndFoldout]
    
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
    
    [SerializeField, ReadOnly, Tooltip("Has this power point been activated at least once")]
    private bool hasBeenActivated = false;
    
    private readonly HashSet<Object> _powerSources = new HashSet<Object>();
    private float _currentRotationSpeed;
    private float _currentLightIntensity;
    private float _currentEmissionIntensity;
    private AudioSource _audioSource;
    
    private Material _pivotMaterial;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        
        // Get the material from the rotation pivot
        if (materialRenderer)
        {
            Renderer rend = materialRenderer;
            if (rend)
            {
                // Create a material instance to avoid changing the shared material
                _pivotMaterial = new Material(rend.material);
                rend.material = _pivotMaterial;
                
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
        
        // Determine if the PowerPoint should be activated
        bool shouldBeOn = (_powerSources.Count >= powerSourcesNeeded) || 
                          (stayActiveAfterFirstActivation && hasBeenActivated);
        
        // Calculate power ratio with an exponential curve
        // When stayActiveAfterFirstActivation is true, and it has been activated, use full power
        float rawRatio;
        if (stayActiveAfterFirstActivation && hasBeenActivated)
        {
            rawRatio = 1.0f;
        }
        else
        {
            rawRatio = Mathf.Clamp01(powerSources / powerSourcesNeeded);
        }
        powerRatio = Mathf.Pow(rawRatio, powerCurve);
        
        // Check activation state changes
        if (shouldBeOn && !isOn && _currentRotationSpeed >= rotationSpeed/3)
        {
            isOn = true;
            hasBeenActivated = true; // Mark as having been activated at least once
            ToggleConnectedCables(true);
            onActivated?.Invoke();
        } 
        else if (!shouldBeOn && isOn && _currentRotationSpeed <= rotationSpeed/2)
        {
            isOn = false;
            ToggleConnectedCables(false);
            onDeactivated?.Invoke();
        }
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
            // Calculate target light intensity based on power ratio, accounting for minimum intensity
            float targetLightIntensity = minLightIntensity + (maxLightIntensity - minLightIntensity) * powerRatio;
            
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
                sfxLoop?.Play(_audioSource);
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
    
    public void ResetActivationState()
    {
        // Method to manually reset the "has been activated" state
        hasBeenActivated = false;
    }
    
    private void ToggleConnectedCables(bool OnActivate)
    {
        if (connectedCables == null || connectedCables.Length == 0) return;

        foreach (var cable in connectedCables)
        {
            if (cable == null) continue;

            if (OnActivate)
            {
                switch (cableStateOnActivate)
                {
                    case CableState.On:
                        cable.SetState(true);
                        break;
                    case CableState.Off:
                        cable.SetState(false);
                        break;
                    case CableState.Toggle:
                        cable.Toggle();
                        break;
                }
            }
            else
            {
                switch (cableStateOnDeactivate)
                {
                    case CableState.On:
                        cable.SetState(true);
                        break;
                    case CableState.Off:
                        cable.SetState(false);
                        break;
                    case CableState.Toggle:
                        cable.Toggle();
                        break;
                }
            }

        }
    }
}