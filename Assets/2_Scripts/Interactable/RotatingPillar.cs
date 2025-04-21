using UnityEngine;
using PrimeTween;
using UnityEngine.Serialization;
using VInspector;

[SelectionBase]
public class RotatingPillar : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform pillarTransform;
    [SerializeField, Tooltip("Y-position when inactive (down position)")]
    private float inactiveYPosition;
    
    [Header("Feedback")]
    [SerializeField, Tooltip("Whether to use a delay before changing the pillar state")]
    private bool useDelay = false;
    
    [SerializeField, ShowIf("useDelay"), Range(0f, 10f), Tooltip("Delay in seconds before the pillar changes state")]
    private float stateChangeDelay = 0.5f;
    [EndIf]
    
    [SerializeField, Range(0.1f, 5f), Tooltip("Time to fully move the pillar to active position")]
    private float activateAnimationTime = 1.0f;
    
    [SerializeField, Range(0.1f, 5f), Tooltip("Time to fully move the pillar to inactive position")]
    private float deactivateAnimationTime = 1.0f;
    
    [SerializeField, Tooltip("Ease function to use for the animation")]
    private Ease animationEase = Ease.Linear;
    
    [SerializeField, Tooltip("Sound effect to play when the pillar is activated")]
    private SOAudioEvent activateSfx;
    
    [SerializeField, Tooltip("Sound effect to play when the pillar is deactivated")]
    private SOAudioEvent deactivateSfx;
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private bool isActive = false;
    
    private AudioSource _audioSource;
    private float _activeYPosition;
    private Sequence _animationSequence;
    private Tween _delayTween;
    
    private void Awake()
    {
        // Don't run initialization logic in edit mode
        if (!Application.isPlaying)
            return;
            
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Make sure pillarTransform is assigned
        if (pillarTransform == null)
        {
            pillarTransform = transform;
        }
        
        // Set the active position to the starting transform position
        _activeYPosition = pillarTransform.localPosition.y;
        
        // Set initial position based on isActive state
        Vector3 initialPosition = pillarTransform.localPosition;
        initialPosition.y = isActive ? _activeYPosition : inactiveYPosition;
        pillarTransform.localPosition = initialPosition;
        
        SetPillarActive(isActive);
    }
    
    [Button]
    public void TogglePillar()
    {
        SetPillarActive(!isActive);
    }
    
    private void OnValidate()
    {
        // Make sure setting changes reflect immediately in edit mode
        if (!Application.isPlaying && pillarTransform != null)
        {
            // Update active position if this is the initial setup
            if (_activeYPosition == 0)
            {
                _activeYPosition = pillarTransform.localPosition.y;
            }
            
            // Update position based on active state
            Vector3 position = pillarTransform.localPosition;
            position.y = isActive ? _activeYPosition : inactiveYPosition;
            pillarTransform.localPosition = position;
        }
    }
    
    public void SetPillarActive()
    {
        SetPillarActive(true);
    }
    
    public void SetPillarInactive()
    {
        SetPillarActive(false);
    }
    
    private void SetPillarActive(bool active)
    {
        // Don't run animation logic in edit mode
        if (!Application.isPlaying)
        {
            // Just update the state for editor visibility
            isActive = active;
            
            // If in editor, immediately set the position without animation
            if (pillarTransform != null)
            {
                Vector3 newPosition = pillarTransform.localPosition;
                newPosition.y = active ? _activeYPosition : inactiveYPosition;
                pillarTransform.localPosition = newPosition;
            }
            return;
        }
        
        // Skip if already in desired state and no animation is running
        if (isActive == active && !_animationSequence.isAlive && !_delayTween.isAlive)
        {
            return;
        }
        
        // Update state immediately
        isActive = active;
        
        // Cancel any running animations
        _delayTween.Stop();
        if (_animationSequence.isAlive)
        {
            _animationSequence.Stop();
        }
        
        // If using delay, wait before starting the animation
        if (useDelay && stateChangeDelay > 0)
        {
            _delayTween = Tween.Delay(stateChangeDelay)
                .OnComplete(() => {
                    StartAnimationSequence(active);
                });
        }
        else
        {
            // No delay, start the animation immediately
            StartAnimationSequence(active);
        }
    }
    
    private void StartAnimationSequence(bool active)
    {
        // Don't run animation in edit mode
        if (!Application.isPlaying)
            return;
            
        // Determine animation time based on direction
        float animTime = active ? activateAnimationTime : deactivateAnimationTime;
        
        // Target Y position based on activation state
        float targetYPosition = active ? _activeYPosition : inactiveYPosition;
        
        // Get current position
        Vector3 startPosition = pillarTransform.localPosition;
        Vector3 endPosition = startPosition;
        endPosition.y = targetYPosition;
        
        // For rotation, we'll use EulerAngles directly which is more reliable for full rotations
        Vector3 startRotation = pillarTransform.localEulerAngles;
        Vector3 endRotation = startRotation;
        // Add 360 degrees clockwise for activation, 360 counterclockwise for deactivation
        endRotation.y += active ? 360 : -360;
        
        if (active && activateSfx != null)
        {
            activateSfx?.Play(_audioSource);
        }
        else if (!active && deactivateSfx != null)
        {
            deactivateSfx?.Play(_audioSource);
        }
        // Create a sequence containing both position and rotation animations
        _animationSequence = Sequence.Create()
            // Add position animation to the sequence
            .Group(Tween.LocalPosition(
                target: pillarTransform,
                endValue: endPosition,
                duration: animTime,
                ease: animationEase
            ))
            // Add rotation animation using EulerAngles tween which handles full rotations better
            .Group(Tween.LocalEulerAngles(
                target: pillarTransform,
                startValue: startRotation,
                endValue: endRotation,
                duration: animTime,
                ease: animationEase
            ));
    }
    
    
    
    
    private void OnDrawGizmosSelected()
    {
        if (pillarTransform == null)
            return;
            
        // Get the current position
        Vector3 currentPos = pillarTransform.position;
        Vector3 localPos = pillarTransform.localPosition;
        
        // Calculate the world position of the inactive position
        Vector3 inactiveLocalPos = localPos;
        inactiveLocalPos.y = inactiveYPosition;
        
        // Convert local inactive position to world position
        Vector3 inactiveWorldPos = pillarTransform.parent != null 
            ? pillarTransform.parent.TransformPoint(inactiveLocalPos) 
            : inactiveLocalPos;
            
        // Draw a sphere at the inactive position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(inactiveWorldPos, 0.2f);
        
        // Draw a line from current position to inactive position
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(currentPos, inactiveWorldPos);
        
        // If we have a stored active position, draw it too
        if (Application.isPlaying || _activeYPosition != 0)
        {
            Vector3 activeLocalPos = localPos;
            activeLocalPos.y = _activeYPosition;
            
            // Convert local active position to world position
            Vector3 activeWorldPos = pillarTransform.parent != null 
                ? pillarTransform.parent.TransformPoint(activeLocalPos) 
                : activeLocalPos;
                
            // Draw a sphere at the active position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(activeWorldPos, 0.2f);
            
            // Draw a line between active and inactive positions
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(activeWorldPos, inactiveWorldPos);
        }
    }
}