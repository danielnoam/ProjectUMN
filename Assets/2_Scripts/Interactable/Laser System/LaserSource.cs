using UnityEngine;
using PrimeTween;
using UnityEngine.Serialization;
using VInspector;

[SelectionBase]
[RequireComponent(typeof(AudioSource))]
public class LaserSource : MonoBehaviour
{

    [Header("Beam Configuration")]
    [SerializeField] private LayerMask collisionMask = -1;
    [SerializeField, Min(0)] private float maxBeamLength = 100f;
    [SerializeField, Min(0)] private float beamStartWidth = 0.1f;
    [SerializeField, Min(0)] private float beamEndWidth = 0.05f;
    [SerializeField] private Transform originTransform;
    [SerializeField] private LaserBeam laserBeam;
    [SerializeField] private Material beamMaterial;

    
    [Header("Animation")]
    [SerializeField] private bool useDelay;
    [SerializeField, ShowIf("useDelay"), Range(0f, 10f)] private float stateChangeDelay = 0.5f; [EndIf]
    [SerializeField, Range(0.1f, 5f)] private float activateAnimationTime = 1.0f;
    [SerializeField, Range(0.1f, 5f)] private float deactivateAnimationTime = 1.0f;
    [SerializeField] private Ease animationEase = Ease.Linear;
    
    [Header("Feedback")]
    [SerializeField] private SOAudioEvent activateSfx;
    [SerializeField] private SOAudioEvent deactivateSfx;
    [SerializeField] private SOAudioEvent hitSfx;
    [SerializeField] private Light spotLight;
    [SerializeField] private Light hitLight;
    [SerializeField] private ParticleSystem continuesHitEffect;
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private bool isActive;

    private Vector3 _lastHitCheckPosition;
    private bool _isHittingSomething;
    private AudioSource _baseAudioSource;
    private AudioSource _hitAudioSource;
    private Tween _activationTween;
    private Tween _delayTween;
    private float _currentBeamLength;
    private bool _wasActive;
    private float _spotLightFullIntensity;
    private float _hitLightFullIntensity;

    
    public LayerMask GetCollisionMask()
    {
        return collisionMask;
    }
    
    private void Awake() {
        _baseAudioSource = GetComponent<AudioSource>();
        _hitAudioSource = continuesHitEffect.gameObject.GetComponent<AudioSource>();
        if (spotLight) {
            _spotLightFullIntensity = spotLight.intensity;
            spotLight.intensity = 0f;
        }
    
        if (hitLight) {
            _hitLightFullIntensity = hitLight.intensity;
            hitLight.intensity = 0f;
        }
    
        if (!laserBeam) return;
    
        // Initialize the beam with the start width
        // We'll update the end width during propagation
        laserBeam.SetBeamProperties(beamStartWidth, beamStartWidth, beamMaterial);
    
        // Set initial length
        _currentBeamLength = isActive ? maxBeamLength : 0f;
    
        SetLaserState(isActive);
    }

    private void OnDisable()
    {
        _activationTween.Stop();
        _delayTween.Stop();
        _wasActive = isActive;
        
        CleanupConnectedLasers();
        SetLaserState(false);
    }
    
    private void OnEnable()
    {
        if (_wasActive)
        {
            SetLaserState(true);
        }
    }
    
    private void OnDestroy()
    {
        CleanupConnectedLasers();
    }

    private void Update() {
        if (!laserBeam) return;
        
        if (isActive || _currentBeamLength > 0) {
            if (!originTransform) return;
            
            Vector3 startPosition = originTransform.position;
            Vector3 direction = originTransform.forward;

            // Reset accumulated distance and set max distance to current animated length
            laserBeam.totalDistance = 0f;
            laserBeam.maxTotalDistance = _currentBeamLength;

            // Store the original line renderer enabled state
            bool wasRendererEnabled = laserBeam.lineRenderer && laserBeam.lineRenderer.enabled;

            // Temporarily enable the line renderer if it's disabled, but we're still animating
            if (_currentBeamLength > 0 && !wasRendererEnabled && laserBeam.lineRenderer) {
                laserBeam.lineRenderer.enabled = true;
            }

            // Propagate the beam
            laserBeam.Propagate(startPosition, direction, collisionMask);
            
            // Calculate the total chain length (sum of all segments)
            float totalChainLength = CalculateTotalBeamChainLength(laserBeam);
            
            // If we have a valid chain length, update all beam widths
            if (totalChainLength > 0) {
                UpdateBeamWidths(laserBeam, totalChainLength);
            }
        
            // Handle hit effects after beam propagation
            HandleHitEffects(laserBeam);

            // Force the beam to visually match the animated length
            EnsureBeamMatchesAnimatedLength(laserBeam, _currentBeamLength);

            // Update maxTotalDistance on all connected optical elements
            if (laserBeam.HitOpticalElement) {
                laserBeam.HitOpticalElement.UpdateMaxDistance(laserBeam, _currentBeamLength);
            }

            // Restore original renderer state if we temporarily enabled it
            if (_currentBeamLength > 0 && !wasRendererEnabled && laserBeam.lineRenderer) {
                laserBeam.lineRenderer.enabled = wasRendererEnabled;
            }
        }

        // Lerp the light intensity based on the laser state
        if (spotLight) {
            spotLight.intensity = Mathf.Lerp(0f, _spotLightFullIntensity, _currentBeamLength / maxBeamLength);
        }
    }

    [Button]
    public void ToggleLaser() {
        SetLaserState(!isActive);
    }
    
    public void SetLaserActive() {
        SetLaserState(true);
    }
    
    public void SetLaserInactive() {
        SetLaserState(false);
    }
    
    private void SetLaserState(bool active) {
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
    
    private void EnsureBeamMatchesAnimatedLength(LaserBeam beam, float animatedLength, float processedLength = 0f) {
        if (!beam) return;
        
        // Calculate the current beam segment length
        float segmentLength = Vector3.Distance(beam.startPosition, beam.endPosition);
    
        // Calculate how much of the animated length is left for this segment
        float remainingAnimatedLength = animatedLength - processedLength;
    
        if (segmentLength > remainingAnimatedLength) {
            // If this segment is longer than what's left in our animation, we need to shorten it
            Vector3 direction = (beam.endPosition - beam.startPosition).normalized;
            beam.endPosition = beam.startPosition + direction * remainingAnimatedLength;
        
            // Update the visual representation
            if (beam.lineRenderer) {
                beam.lineRenderer.SetPosition(0, beam.startPosition);
                beam.lineRenderer.SetPosition(1, beam.endPosition);
            }
        
            // This segment consumed all remaining animated length, so we clear any optical elements
            // that might try to continue the beam
            beam.HitOpticalElement = null;
            beam.HitPowerPoint = null;
        } else if (beam.HitOpticalElement) {
            // This segment didn't consume all animated length, and we hit an optical element,
            // so we need to continue checking downstream beams
        
            // Get the next beam in the chain from the optical element
            LaserBeam nextBeam = GetNextBeamInChain(beam);
            if (nextBeam) {
                // Recursively process the next beam in the chain
                EnsureBeamMatchesAnimatedLength(nextBeam, animatedLength, processedLength + segmentLength);
            }
        }
    }
    
    private void StartActivationAnimation(bool active) {
        _activationTween.Stop();
        
        // Determine animation time based on direction
        float animTime = active ? activateAnimationTime : deactivateAnimationTime;
        
        // Target length based on activation state
        float targetLength = active ? maxBeamLength : 0f;
        
        // Animate the laser beam length
        if (active && _baseAudioSource) { 
            activateSfx?.Play(_baseAudioSource); 
        }
        
        _activationTween = Tween.Custom(
            startValue: _currentBeamLength,
            endValue: targetLength,
            duration: animTime,
            ease: animationEase,
            onValueChange: value => {
                _currentBeamLength = value;
                // Make sure the line renderer is enabled when visible
                if (laserBeam && laserBeam.lineRenderer) {
                    laserBeam.lineRenderer.enabled = _currentBeamLength > 0;
                }
            }
        )
        .OnComplete(() => { 
            if (!active && _baseAudioSource) {
                deactivateSfx?.Play(_baseAudioSource);
            } 
        });
    }
    
    private LaserBeam GetNextBeamInChain(LaserBeam beam) {
        if (!beam || !beam.HitOpticalElement) return null;
        
        if (beam.HitOpticalElement is LaserMirror mirror) {
            return mirror.LaserBeamPairs.Find(x => x.incoming == beam)?.outgoing;
        } else if (beam.HitOpticalElement is LaserPortal portal) {
            return portal.LaserBeamPairs.Find(x => x.incoming == beam)?.outgoing;
        }
        return null;
    }
    
    private void CleanupConnectedLasers()
    {
        if (!laserBeam) return;
        
        // Start cleanup from the first beam
        if (laserBeam.HitOpticalElement)
        {
            // Tell the optical element to clean up its connected beams
            laserBeam.HitOpticalElement.CleanupConnectedBeams();
        
            // Clear the reference to the optical element
            laserBeam.HitOpticalElement = null;
        }
    
        // Also clear any PowerPoint connection
        if (laserBeam.HitPowerPoint)
        {
            laserBeam.HitPowerPoint = null;
        }
    }
    
    private void HandleHitEffects(LaserBeam beam) {
        if (!continuesHitEffect || !beam) return;
        
        // Find the last beam in the chain
        LaserBeam finalBeam = FindFinalBeam(beam);
        if (!finalBeam) return;
        
        // Store the position for gizmo visualization
        _lastHitCheckPosition = finalBeam.endPosition;
        
        // Perform a sphere collision check at the end position of the final beam
        _isHittingSomething = Physics.CheckSphere(
            finalBeam.endPosition, 
            0.01f,  // Small radius for actual check
            collisionMask
        );
        
        if (_isHittingSomething) {
            // Position and orient the hit effect
            continuesHitEffect.transform.position = finalBeam.endPosition;
            continuesHitEffect.transform.rotation = Quaternion.LookRotation(finalBeam.hitNormal);
            
            if (!continuesHitEffect.isPlaying) {
                continuesHitEffect.Play();
            }
            
            // Play hit sound effect
            if (_hitAudioSource && !_hitAudioSource.isPlaying) {
                hitSfx?.Play(_hitAudioSource, Random.value);
            }
            
            // Set the hit light intensity
            if (hitLight) {
                hitLight.intensity = _hitLightFullIntensity;
            }
            
        } else {
            // Not hitting anything, stop the effect
            if (continuesHitEffect.isPlaying) {
                continuesHitEffect.Stop();
                continuesHitEffect.Clear();
            }
            
            // Stop the hit sound effect
            if (_hitAudioSource && _hitAudioSource.isPlaying) {
                hitSfx?.Stop(_hitAudioSource);
            }
            
            // Reset the hit light intensity
            if (hitLight) {
                hitLight.intensity = 0f;
            }
        }
        
    }


    private LaserBeam FindFinalBeam(LaserBeam beam) {
        if (!beam) return null;
        
        // If this beam hits an optical element, follow the chain
        if (beam.HitOpticalElement) {
            LaserBeam nextBeam = GetNextBeamInChain(beam);
            if (nextBeam) {
                return FindFinalBeam(nextBeam);
            }
        }
        return beam;
    }
    
    private void UpdateBeamWidths(LaserBeam beam, float totalLength, float processedLength = 0f) {
        if (!beam || !beam.lineRenderer) return;
    
        float segmentLength = Vector3.Distance(beam.startPosition, beam.endPosition);
        float startRatio = processedLength / totalLength;
        float endRatio = (processedLength + segmentLength) / totalLength;
    
        // Linearly interpolate between start and end width based on position in the chain
        float startWidth = Mathf.Lerp(beamStartWidth, beamEndWidth, startRatio);
        float endWidth = Mathf.Lerp(beamStartWidth, beamEndWidth, endRatio);
    
        // Update this segment's widths
        beam.SetBeamWidth(startWidth, endWidth);
    
        // If this segment hits an optical element, continue updating downstream
        if (beam.HitOpticalElement) {
            LaserBeam nextBeam = GetNextBeamInChain(beam);
            if (nextBeam) {
                UpdateBeamWidths(nextBeam, totalLength, processedLength + segmentLength);
            }
        }
    }
    
    private float CalculateTotalBeamChainLength(LaserBeam beam, float accumulatedLength = 0f) {
        if (!beam) return accumulatedLength;
    
        float segmentLength = Vector3.Distance(beam.startPosition, beam.endPosition);
        float newAccumulatedLength = accumulatedLength + segmentLength;
    
        // If this beam hits an optical element, continue the calculation
        if (beam.HitOpticalElement) {
            LaserBeam nextBeam = GetNextBeamInChain(beam);
            if (nextBeam) {
                return CalculateTotalBeamChainLength(nextBeam, newAccumulatedLength);
            }
        }
    
        return newAccumulatedLength;
    }
    
    
    private void OnDrawGizmosSelected() {

        Gizmos.color = _isHittingSomething ? Color.red: Color.yellow;
        
        // Draw a sphere at the last check position
        Gizmos.DrawWireSphere(_lastHitCheckPosition, 0.2f);
            
        // Draw a small solid sphere in the center
        Gizmos.DrawWireSphere(_lastHitCheckPosition, 0.2f);
        
    }
}