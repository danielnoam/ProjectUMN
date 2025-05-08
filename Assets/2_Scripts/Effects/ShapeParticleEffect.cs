using System;
using System.Collections.Generic;
using Shapes;
using UnityEngine;
using VInspector;
using Random = UnityEngine.Random;


public class ShapeParticleEffect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float baseCycleDuration = 0.3f;
    [SerializeField] private float randomCycleVariation = 0.2f;
    [SerializeField] private float randomScaleMin = -0.5f;
    [SerializeField] private float randomScaleMax = 0.5f;
    [SerializeField] private bool randomizeNextShape = true;
    [SerializeField] private ShapeGroup[] shapesToDisplay;

    
    [Header("Alpha Settings")]
    [SerializeField] private float minAlpha = 0f;
    [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private bool randomizeAlpha = true;
    [SerializeField] private float alphaVariation = 0.2f;
    
    [Header("Hover Animation")]
    [SerializeField] private bool hover = true;
    [SerializeField] private float hoverDistance = 0.5f;
    [SerializeField] private float hoverSpeed = 1f;
    
    [Header("Rotation To Camera")]
    [SerializeField] private bool lookAtCamera = true;
    [SerializeField] private bool rotateX = true;
    [SerializeField] private bool rotateY = true;
    [SerializeField] private bool rotateZ;
    [SerializeField] private bool smoothRotation = true;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    
    [Header("Distance Scaling Based On Camera")]
    [SerializeField] private bool scaleWithDistance = true;
    [SerializeField] private bool smoothScaling = true;
    [SerializeField] private float minDistance = 7f;
    [SerializeField] private float maxDistance = 18f;
    [SerializeField] private float scalingSpeed = 10f;
    
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SOAudioEvent simulationSfx;
    
    
    private Camera _targetCamera;
    private Vector3 _originalScale;
    private Vector3 _originalPosition;
    private Vector3 _originalRotation;
    private readonly Dictionary<ShapeGroup, Color> _shapesDefaultColors = new Dictionary<ShapeGroup, Color>();
    private int _currentShapeIndex;
    private int _previousShapeIndex;
    
    // Variables for random cycle timing
    private float _nextCycleTime;
    private float _currentCycleDuration;
    
    // Variables for alpha customization
    private float _currentMinAlpha;
    private float _currentMaxAlpha;

    private void Awake()
    {
        _originalRotation = transform.eulerAngles;
        _originalScale = transform.localScale + (Vector3.one * Random.Range(randomScaleMin, randomScaleMax));
        _originalPosition = transform.localPosition;
        
        
        // for each shape group in shapesToDisplay save the default alpha value
        foreach (var shapeGroup in shapesToDisplay)
        {
            if (shapeGroup)
            {
                _shapesDefaultColors[shapeGroup] = shapeGroup.Color;
                SetAlpha(shapeGroup, 0f);
            }
        }
        
        // Set a random starting shape
        if (shapesToDisplay.Length > 0)
        {
            _currentShapeIndex = Random.Range(0, shapesToDisplay.Length);
            _previousShapeIndex = _currentShapeIndex;
            // Set the active shape's alpha to 1 initially
            if (shapesToDisplay[_currentShapeIndex])
            {
                SetAlpha(shapesToDisplay[_currentShapeIndex], maxAlpha);
            }
        }
        
        // Initialize first cycle time with randomness
        SetNextCycleTime();
    }
    
    private void Start()
    {
        if (!_targetCamera)
        {
            _targetCamera = Camera.main;
        }
        
        simulationSfx?.Play(audioSource, Random.Range(0,1f));
    }

    // Method to set the next cycle time with randomness
    private void SetNextCycleTime()
    {
        // Generate a random duration within the specified range
        _currentCycleDuration = baseCycleDuration + Random.Range(-randomCycleVariation, randomCycleVariation);
        
        // Ensure the duration is always positive
        _currentCycleDuration = Mathf.Max(0.05f, _currentCycleDuration);
        
        // Set the next cycle time
        _nextCycleTime = Time.time + _currentCycleDuration;
        
        // Randomize alpha values if enabled
        if (randomizeAlpha)
        {
            // Generate random min alpha within bounds
            _currentMinAlpha = minAlpha + Random.Range(0, alphaVariation);
            
            // Generate random max alpha within bounds
            _currentMaxAlpha = maxAlpha - Random.Range(0, alphaVariation);
            
            // Make sure min doesn't exceed max
            _currentMinAlpha = Mathf.Min(_currentMinAlpha, _currentMaxAlpha - 0.1f);
        }
        else
        {
            // Use the set values
            _currentMinAlpha = minAlpha;
            _currentMaxAlpha = maxAlpha;
        }
    }

    private void Update()
    {
        if (shapesToDisplay.Length > 0)
        {
            // Cycle through the shapes with random timing
            // Check if it's time for the next cycle
            if (Time.time >= _nextCycleTime)
            {
                // Set current shape alpha to 0
                if (_currentShapeIndex >= 0 && _currentShapeIndex < shapesToDisplay.Length)
                {
                    SetAlpha(shapesToDisplay[_currentShapeIndex], 0f);
                }
                
                _previousShapeIndex = _currentShapeIndex;
                
                // Update to the next shape (either sequential or random)
                if (randomizeNextShape)
                {
                    // If we only have 1 shape, just keep using it
                    if (shapesToDisplay.Length == 1)
                    {
                        _currentShapeIndex = 0;
                    }
                    else
                    {
                        // Keep generating random indices until we get one different from the previous
                        int newIndex;
                        do
                        {
                            newIndex = Random.Range(0, shapesToDisplay.Length);
                        } while (newIndex == _previousShapeIndex && shapesToDisplay.Length > 1);
                        
                        _currentShapeIndex = newIndex;
                    }
                }
                else
                {
                    // Sequential cycling
                    _currentShapeIndex = (_currentShapeIndex + 1) % shapesToDisplay.Length;
                }
                
                // Set the next cycle time with randomness
                SetNextCycleTime();
            }
            
            // Calculate how far we are through the current cycle (0 to 1)
            float cycleProgress = 1.0f - ((_nextCycleTime - Time.time) / _currentCycleDuration);
            cycleProgress = Mathf.Clamp01(cycleProgress);
            
            // Use PingPong to create a fade in/out effect based on the cycle progress
            // Map the pingpong from 0-1 to our custom min-max alpha range
            float normalizedAlpha = Mathf.PingPong(cycleProgress * 2.0f, 1.0f);
            float mappedAlpha = Mathf.Lerp(_currentMinAlpha, _currentMaxAlpha, normalizedAlpha);
            
            // Apply the alpha to the current shape
            if (_currentShapeIndex >= 0 && _currentShapeIndex < shapesToDisplay.Length)
            {
                ShapeGroup currentShape = shapesToDisplay[_currentShapeIndex];
                if (currentShape)
                {
                    Color originalColor = _shapesDefaultColors[currentShape];
                    SetAlpha(currentShape, mappedAlpha * originalColor.a);
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (hover)
        {
            // Calculate the new position using a sine wave for hover effect
            float newY = _originalPosition.y + Mathf.Sin(Time.time * hoverSpeed) * hoverDistance;
            transform.localPosition = new Vector3(_originalPosition.x, newY, _originalPosition.z);
        }
        
        if (_targetCamera)
        {
            // Get the direction from the object to the camera
            Vector3 directionToCamera = _targetCamera.transform.position - transform.position;
            
            // Calculate distance to camera for scaling
            float distanceToCamera = directionToCamera.magnitude;
            
            if (lookAtCamera)
            {
                // Create a rotation that looks at the camera
                Quaternion lookRotation = Quaternion.LookRotation(directionToCamera);
            
                // Extract the Euler angles
                Vector3 eulerRotation = lookRotation.eulerAngles;
                
                // Selectively apply rotations based on which axes we want to rotate
                if (!rotateX) eulerRotation.x = _originalRotation.x;
                if (!rotateY) eulerRotation.y = _originalRotation.y;
                if (!rotateZ) eulerRotation.z = _originalRotation.z;
            
                // Apply any rotation offset
                eulerRotation += rotationOffset;
            
                // Convert back to quaternion
                Quaternion targetRotation = Quaternion.Euler(eulerRotation);
            
                // Apply the rotation (with or without smoothing)
                if (smoothRotation)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                else
                {
                    transform.rotation = targetRotation;
                }
            }

            // Apply distance-based scaling if enabled
            if (scaleWithDistance)
            {
                // Calculate scale factor based on distance (clamped between min and max distance)
                float normalizedDistance = Mathf.Clamp(distanceToCamera, minDistance, maxDistance);
                float scaleFactor = normalizedDistance / minDistance;
                
                // Calculate target scale
                Vector3 targetScale = _originalScale * scaleFactor;
                
                // Apply scaling with or without smoothing
                if (smoothScaling)
                {
                    transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scalingSpeed * Time.deltaTime);
                }
                else
                {
                    transform.localScale = targetScale;
                }
            }
        }
    }
    
    private void SetAlpha(ShapeGroup graphic, float alpha)
    {
        if (!graphic) return;
        
        Color color = graphic.Color;
        color.a = alpha;
        graphic.Color = color;
    }
    
    
    [Button] private void AddAllShapeGroups()
    {
        shapesToDisplay = Array.Empty<ShapeGroup>();
        shapesToDisplay = GetComponentsInChildren<ShapeGroup>();
        foreach (var shapeGroup in shapesToDisplay)
        {
            if (shapeGroup)
            {
                _shapesDefaultColors[shapeGroup] = shapeGroup.Color;
            }
        }
    }
}