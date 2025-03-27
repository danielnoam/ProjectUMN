using UnityEngine;
using PrimeTween;
using VInspector;

[SelectionBase]
[RequireComponent(typeof(AudioSource))]
public class LaserSource : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform originTransform;
    [SerializeField] private LaserBeam laserBeam;
    [SerializeField] private float beamLength = 100f;
    [SerializeField] private float beamWidth = 0.1f;
    [SerializeField] private Color beamColor = Color.red;
    [SerializeField] private Material beamMaterial;
    [SerializeField] private LayerMask collisionMask = -1;
    
    [Header("Feedback")]
    [SerializeField, Tooltip("Whether to use a delay before changing the laser state")]
    private bool useDelay = false;
    
    [SerializeField, ShowIf("useDelay"), Range(0f, 10f), Tooltip("Delay in seconds before the laser changes state")]
    private float stateChangeDelay = 0.5f;
    [EndIf]
    
    [SerializeField, Range(0.1f, 5f), Tooltip("Time to fully activate the laser")]
    private float activateAnimationTime = 1.0f;
    
    [SerializeField, Range(0.1f, 5f), Tooltip("Time to fully deactivate the laser")]
    private float deactivateAnimationTime = 1.0f;
    
    [SerializeField, Tooltip("Ease function to use for the animation")]
    private Ease animationEase = Ease.Linear;
    
    [SerializeField, Tooltip("Sound effect to play when the laser is activated")]
    private SOAudioEvent activateSfx;
    
    [SerializeField, Tooltip("Sound effect to play when the laser is deactivated")]
    private SOAudioEvent deactivateSfx;
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private bool isActive = false;
    
    private AudioSource _audioSource;
    private Tween _activationTween;
    private Tween _delayTween;
    private float _currentBeamLength = 0f;
    
    private void Awake() {
        _audioSource = GetComponent<AudioSource>();
        
        // Initialize the beam with the specified properties
        laserBeam.SetBeamProperties(beamWidth, beamColor, beamMaterial);
        
        // Set initial length
        _currentBeamLength = isActive ? beamLength : 0f;
    }
    
    private void Update() {
        if (isActive || _currentBeamLength > 0) {
            Vector3 startPosition = originTransform.position;
            Vector3 direction = originTransform.forward;
            
            // Reset accumulated distance and set max distance to current animated length
            laserBeam.totalDistance = 0f;
            laserBeam.maxTotalDistance = _currentBeamLength;
            laserBeam.Propagate(startPosition, direction, collisionMask);
        }
    }
    
    [Button]
    public void ToggleLaser() {
        SetLaserActive(!isActive);
    }
    
    public void SetLaserActive(bool active) {
        // Skip if already in desired state and no animation is running
        if (isActive == active && !_activationTween.isAlive && !_delayTween.isAlive) {
            return;
        }
        
        // Update state immediately
        isActive = active;
        
        // Cancel any running animations
        _delayTween.Stop();
        
        // If using delay, wait before starting the animation
        if (useDelay && stateChangeDelay > 0) {
            _delayTween = Tween.Delay(stateChangeDelay)
                .OnComplete(() => {
                    StartActivationAnimation(active);
                });
        } else {
            // No delay, start the animation immediately
            StartActivationAnimation(active);
        }
    }
    
    private void StartActivationAnimation(bool active) {
        // Stop any current animation
        _activationTween.Stop();
        
        // Determine animation time based on direction
        float animTime = active ? activateAnimationTime : deactivateAnimationTime;
        
        // Target length based on activation state
        float targetLength = active ? beamLength : 0f;
        
        // Animate the laser beam length
        _activationTween = Tween.Custom(
            startValue: _currentBeamLength,
            endValue: targetLength,
            duration: animTime,
            ease: animationEase,
            onValueChange: value => {
                _currentBeamLength = value;
                // Make sure the line renderer is enabled when visible
                if (laserBeam._lineRenderer != null) {
                    laserBeam._lineRenderer.enabled = _currentBeamLength > 0;
                }
            }
        )
        .OnComplete(() => {
            // Play sound effect when animation completes
            if (active) {
                activateSfx?.Play(_audioSource);
            } else {
                deactivateSfx?.Play(_audioSource);
            }
        });
    }
}