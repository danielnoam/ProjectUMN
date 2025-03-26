using UnityEngine;
using VInspector;
using PrimeTween;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[SelectionBase]
[ExecuteInEditMode]
public class PowerPlane : MonoBehaviour
{
    [Header("Plane Settings")]
    [SerializeField, Range(0.1f, 15f), Tooltip("Width of the plane")]
    private float planeWidth = 2f;
    
    [SerializeField, Range(0.1f, 15f), Tooltip("Height/thickness of the plane")]
    private float planeHeight = 0.2f;
    
    [SerializeField, Range(0.1f, 5f), Tooltip("Time to fully activate or deactivate the plane")]
    private float animationTime = 2.0f;

    [SerializeField, Tooltip("Ease function to use for the animation")]
    private Ease animationEase = Ease.Linear;
    [SerializeField, Tooltip("Whether to use a delay before changing the power plane state")]
    private bool useDelay = false;
    
    [EnableIf("useDelay")]
    [SerializeField, Range(0f, 10f), Tooltip("Delay in seconds before the plane changes state")]
    private float stateChangeDelay = 0.5f;
    [EndIf]
    

    
    [Header("Runtime")]
    [SerializeField, Tooltip("Toggle to activate/deactivate the plane (works in editor and play mode)")]
    private bool isActive;
    
    [SerializeField, Tooltip("When toggled on, activation calls will turn the plane on. When toggled off, activation calls will turn the plane off.")]
    private bool powerTurnsOn = true;
    
    
    [Header("References")]
    [SerializeField, Tooltip("Starting point of the plane")]
    private Transform startPoint;
    
    [SerializeField, Tooltip("Ending point of the plane")]
    private Transform endPoint;
    
    [SerializeField, Tooltip("Reference to the visual GameObject that will be scaled between start and end points")]
    private Transform planeVisualRef;
    
    [SerializeField, Tooltip("Material to apply to the plane visual")]
    private Material planeMaterial;

    
    [SerializeField]
    private AudioSource audioSource;
    
    [SerializeField]
    private AudioSource audioSource2;
    
    [SerializeField]
    private SOAudioEvent sfxPlaneActivated;
    
    [SerializeField]
    private SOAudioEvent sfxPlaneDeactivated;
    
    
    private GameObject _planeObject;
    private BoxCollider _boxCollider;
    private Renderer _renderer;
    private Tween _activationTween;
    private Tween _delayTween;
    private float _currentLength;
    private float _targetLength;
    
    // HashSet to track which objects have activated the plane
    private readonly HashSet<Object> _activatingObjects = new HashSet<Object>();
    
    
    
    private void Awake()
    {
        if (!isActive) audioSource2?.Stop();
        InitializeComponents();
    }
    
    private void Start()
    {
        // Only proceed if we have a valid visual reference
        if (!planeVisualRef) return;
        
        // Set initial state without animation
        _currentLength = isActive ? GetFullLength() : 0f;
        _targetLength = _currentLength; // Initialize target length
        UpdatePlaneTransform(_currentLength);
        
        // Set renderer state
        UpdateComponentStates();
    }
    
    private void Update()
    {
        // Only proceed if we have a valid visual reference
        if (!planeVisualRef) return;
        
        // Only update if points exist and if positions have changed
        if (startPoint && endPoint && 
           (startPoint.hasChanged || endPoint.hasChanged))
        {
            // Update full length measurement
            float fullLength = GetFullLength();
            
            // If active or animating to active, update target and current length proportionally
            if (isActive)
            {
                _targetLength = fullLength;
                
                // If not in the middle of an animation, update current length too
                if (!_activationTween.isAlive)
                {
                    _currentLength = fullLength;
                }
                else
                {
                    // Maintain the same animation progress when endpoints move during animation
                    float animProgress = _activationTween.progress;
                    _currentLength = Mathf.Lerp(0f, fullLength, animProgress);
                }
            }
            
            UpdatePlaneTransform(_currentLength);
            startPoint.hasChanged = false;
            endPoint.hasChanged = false;
        }
    }
    
    private void OnEnable()
    {
        if (_planeObject)
            _planeObject.SetActive(true);
    }
    
    private void OnDisable()
    {
        if (_planeObject)
            _planeObject.SetActive(false);
    }
    
    
        
    #region Control Methods -----------------------------------------------------------------------------------
    
    [Button]
    private void TogglePlane()
    {
        // When called from inspector, use this component as the caller
        TogglePlane(this);
    }
    
    public void TogglePlane(Object caller)
    {
        if (_activatingObjects.Contains(caller))
        {
            DeactivatePlane(caller);
        }
        else
        {
            ActivatePlane(caller);
        }
    }
    
    private void ActivatePlane()
    {
        // When called from inspector, use this component as the caller
        ActivatePlane(this);
    }
    
    public void ActivatePlane(Object caller)
    {
        if (caller == null)
        {
            Debug.LogWarning("PowerPlane: Caller cannot be null when activating");
            return;
        }
        
        // Add to the set of activating objects
        _activatingObjects.Add(caller);
        
        // Update the activation state based on powerTurnsOn
        UpdateActivationState();
    }
    
    private void DeactivatePlane()
    {
        // When called from inspector, use this component as the caller
        DeactivatePlane(this);
    }
    
    public void DeactivatePlane(Object caller)
    {
        if (caller == null)
        {
            Debug.LogWarning("PowerPlane: Caller cannot be null when deactivating");
            return;
        }
        
        // Remove from the set of activating objects
        _activatingObjects.Remove(caller);
        
        // Update the activation state
        UpdateActivationState();
    }
    
    private void UpdateActivationState()
    {
        bool shouldBeActive = _activatingObjects.Count > 0;
        
        // If powerTurnsOn is false, we invert the activation logic
        if (!powerTurnsOn)
            shouldBeActive = !shouldBeActive;
            
        SetPlaneActive(shouldBeActive);
    }
    
    [Button]
    public void ToggleGlobalState()
    {
        powerTurnsOn = !powerTurnsOn;
        UpdateActivationState();
    }
    
    #endregion Control Methods -----------------------------------------------------------------------------------
    
    
    
    #region Setup Methods --------------------------------------------------------------------------------------
    
    private void InitializeComponents()
    {
        if (planeVisualRef)
        {
            _planeObject = planeVisualRef.gameObject;
            
            // Get the existing renderer if any
            _renderer = _planeObject.GetComponent<Renderer>();
            if (!_renderer)
            {
                // Try to find renderer in children
                _renderer = _planeObject.GetComponentInChildren<Renderer>();
            }
            
            // Only use existing BoxCollider, don't add our own
            _boxCollider = _planeObject.GetComponentInChildren<BoxCollider>();
            
            // Set material if provided
            if (planeMaterial && _renderer)
            {
                if (Application.isPlaying)
                    _renderer.material = planeMaterial;
                else
                    _renderer.sharedMaterial = planeMaterial;
            }
        }
    }
    
    private float GetFullLength()
    {
        if (!startPoint || !endPoint)
            return 0f;
            
        return Vector3.Distance(startPoint.position, endPoint.position);
    }
    
    private void UpdatePlaneTransform(float length)
    {
        if (!startPoint || !endPoint || !planeVisualRef)
            return;
            
        // Calculate direction and position
        Vector3 startPos = startPoint.position;
        Vector3 direction = endPoint.position - startPos;
        
        // If points are too close, set a minimal size
        if (direction.magnitude < 0.001f)
        {
            planeVisualRef.localScale = new Vector3(planeWidth, planeHeight, 0.001f);
            planeVisualRef.position = startPos;
            
            if (_boxCollider)
            {
                _boxCollider.size = Vector3.one;
                _boxCollider.center = Vector3.zero;
            }
            
            return;
        }
        
        // Normalize direction
        direction.Normalize();
        
        // Set the position to be at the midpoint of the visible part
        Vector3 midPoint = startPos + direction * (length * 0.5f);
        planeVisualRef.position = midPoint;
        
        // Set the rotation to look at the end point
        planeVisualRef.rotation = Quaternion.LookRotation(direction);
        
        // Set the scale
        planeVisualRef.localScale = new Vector3(planeWidth, planeHeight, length);
        
        // Update the collider if it exists
        if (_boxCollider)
        {
            _boxCollider.size = Vector3.one; // The collider automatically scales with the transform
            _boxCollider.center = Vector3.zero;
        }
    }
    
    private void UpdateComponentStates()
    {
        if (_renderer) _renderer.enabled = isActive || _activationTween is { isAlive: true, progress: > 0 };
    }
    
    private void SetPlaneActive(bool active)
    {
        // Skip if no visual reference is set
        if (!planeVisualRef)
        {
            Debug.LogWarning("PowerPlane: Cannot activate/deactivate - no planeVisualRef assigned");
            return;
        }
            
        // Skip if already in desired state and no animation is running
        if (isActive == active && !_activationTween.isAlive && !_delayTween.isAlive)
            return;
            
        // Calculate target length based on the new state
        float fullLength = GetFullLength();
        float newTargetLength = active ? fullLength : 0f;
        _targetLength = newTargetLength;
        
        // Update the target state
        isActive = active;


        
        // Make sure the renderer is enabled during animation
        if (_renderer)
            _renderer.enabled = true;
        
        // Only animate in play mode
        if (Application.isPlaying && gameObject.activeInHierarchy)
        {
            // Stop any running delay
            _delayTween.Stop();
            
            // If using delay, wait before starting the state change
            if (useDelay && stateChangeDelay > 0)
            {
                // Create delay tween
                _delayTween = Tween.Delay(stateChangeDelay)
                    .OnComplete(() => {
                        // Start the state change animation
                        StartStateChangeAnimation(active);
                    });
            }
            else
            {
                // No delay, start the state change immediately
                StartStateChangeAnimation(active);
            }
        }
        else
        {
            // Immediately set the state without animation
            _currentLength = newTargetLength;
            UpdatePlaneTransform(_currentLength);
            UpdateComponentStates();
        }
        
    }
    
    private void StartStateChangeAnimation(bool active)
    {
        // Calculate animation duration based on current progress
        float fullLength = GetFullLength();
        float targetLength = active ? fullLength : 0.05f;
        
        // Stop current animation if running
        _activationTween.Stop();
        
        // Calculate remaining animation time based on how far we need to go
        float remainingDistance = Mathf.Abs(targetLength - _currentLength);
        float totalDistance = fullLength; // Total possible distance to travel
        
        // Calculate what percentage of the total animation we need to perform
        float animationPercentage = totalDistance > 0 ? remainingDistance / totalDistance : 0;
        
        // Scale animation time by the percentage of distance we need to cover
        float scaledAnimTime = animationTime * animationPercentage;
        
        // Ensure we have a minimum animation time to avoid visual glitches
        scaledAnimTime = Mathf.Max(scaledAnimTime, 0.05f);
        
        // Start the animation
        if (active) audioSource2?.Play();
        
        _activationTween = Tween.Custom(
            startValue: _currentLength,
            endValue: targetLength,
            duration: scaledAnimTime,
            ease: animationEase,
            onValueChange: val => {
                _currentLength = val;
                UpdatePlaneTransform(val);
            }
        )
        .OnComplete(() =>         
            {
                UpdateComponentStates();
                
                if (active)
                {
                    sfxPlaneActivated?.Play(audioSource);
                }
                else
                {
                    sfxPlaneDeactivated?.Play(audioSource);
                    audioSource2?.Stop();
                }
                
                
            });
    }
    

    
    #endregion Setup Methods --------------------------------------------------------------------------------------

    
    
    #region Editor -----------------------------------------------------------------------------------

        private void OnValidate()
    {
        // If the visual reference changed, reinitialize
        if (planeVisualRef && (_planeObject == null || planeVisualRef.gameObject != _planeObject))
        {
            _planeObject = null; // Force reinitialization
            InitializeComponents();
        }
        
#if UNITY_EDITOR
        // Queue delayed update to avoid "SendMessage cannot be called during" errors
        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += () => 
            {
                if (this == null || gameObject == null) return;
                
                // Update based on current state
                if (planeVisualRef)
                {
                    _currentLength = isActive ? GetFullLength() : 0f;
                    _targetLength = _currentLength; // Set target length as well
                    UpdatePlaneTransform(_currentLength);
                    UpdateComponentStates();
                }
            };
        }
#endif
    }
    
    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!startPoint || !endPoint)
            return;
            
        
        if (!isActive)
        {
            // Draw a line showing the plane path
            Gizmos.color = Color.red;
            Gizmos.DrawLine(startPoint.position, endPoint.position);
            
            // Draw the plane bounds
            Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan with transparency
            Vector3 direction = endPoint.position - startPoint.position;
            float fullLength = direction.magnitude;
            Vector3 center = startPoint.position + direction.normalized * (isActive ? fullLength * 0.5f : 0f);
        
            // Draw plane bounds
            Matrix4x4 originalMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                center,
                Quaternion.LookRotation(direction.normalized),
                new Vector3(planeWidth, planeHeight, isActive ? fullLength : 0f)
            );
            
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = originalMatrix;
        }
        
        // Draw spheres at start and end
        Gizmos.DrawSphere(startPoint.position, 0.2f);
        Gizmos.DrawSphere(endPoint.position, 0.2f);
        

        
    }
    #endif
    

    #endregion Editor -----------------------------------------------------------------------------------
    
    

}