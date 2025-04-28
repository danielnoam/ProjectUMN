using System;
using UnityEngine;
using UnityEngine.Serialization;
using VInspector;

[RequireComponent(typeof(LineRenderer))]
public class LaserBeam : MonoBehaviour
{
    [HideInInspector] public float totalDistance = 0f;
    [HideInInspector] public float maxTotalDistance = 100f;
    
    public Vector3 startPosition;
    public Vector3 endPosition;
    public Vector3 hitNormal;
    public LaserBeam prefab;
    public Vector3 Direction => (endPosition - startPosition).normalized;
    private LaserOpticalElementBase _hitOpticalElement;
    private PowerPoint _hitPowerPoint;
    [HideInInspector] public LineRenderer lineRenderer;

    public LaserOpticalElementBase HitOpticalElement { 
        get => _hitOpticalElement; 
        set {
            if (_hitOpticalElement == value) return;
            
            if (_hitOpticalElement) {
                _hitOpticalElement.UnregisterLaserBeam(this);
            }

            _hitOpticalElement = value;

            if (_hitOpticalElement) {
                _hitOpticalElement.RegisterLaserBeam(this);
            }
        }
    }
    
    public PowerPoint HitPowerPoint {
        get => _hitPowerPoint;
        set {
            if (_hitPowerPoint == value) return;
            
            if (_hitPowerPoint) {
                _hitPowerPoint.RemovePowerSource(this);
            }
            
            _hitPowerPoint = value;
            
            if (_hitPowerPoint) {
                _hitPowerPoint.AddPowerSource(this);
            }
        }
    }

    private void Awake() {                                                                                                               
        lineRenderer = GetComponent<LineRenderer>();
        if (!lineRenderer) return;
        
        lineRenderer.positionCount = 2;
    }

    private void OnDestroy()
    {
        if (_hitOpticalElement) {
            _hitOpticalElement.UnregisterLaserBeam(this);
        }

        if (_hitPowerPoint) {
            _hitPowerPoint.RemovePowerSource(this);
        }
    }

    public void SetBeamProperties(float width, Color color, Material material) {
        if (!lineRenderer) return;
        
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        if (material) lineRenderer.material = material;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    public void Propagate(Vector3 startPosition, Vector3 direction, LayerMask layerMask) {
        // Remember the original totalDistance before we add this segment
        float originalDistance = totalDistance;
        
        // Calculate how much distance we have left
        float remainingDistance = maxTotalDistance - totalDistance;
        
        // Check if we've exceeded total beam distance
        if (remainingDistance <= 0) {
            // No more propagation, just make this a zero-length beam
            this.startPosition = startPosition;
            this.endPosition = startPosition;
            UpdateVisuals();
            return;
        }

        Vector3 endPosition = startPosition + direction * remainingDistance;
        Vector3 hitNormal = Vector3.zero;

        if (Physics.Raycast(startPosition, direction, out RaycastHit hit, remainingDistance, layerMask)) {
            endPosition = hit.point;
            hitNormal = hit.normal;

            // Check for optical element
            HitOpticalElement = hit.collider.TryGetComponent(out LaserOpticalElementBase opticalElement) ? opticalElement : null;
            
            // Check for PowerPoint
            HitPowerPoint = hit.collider.TryGetComponent(out PowerPoint powerPoint) ? powerPoint : null;
        }
        else {
            HitOpticalElement = null;
            HitPowerPoint = null;
        }

        this.startPosition = startPosition;
        this.endPosition = endPosition;
        this.hitNormal = hitNormal;
        
        float segmentLength = Vector3.Distance(startPosition, endPosition);
        totalDistance += segmentLength;
        
        UpdateVisuals();

        if (HitOpticalElement) {
            // Pass this beam to the optical element
            HitOpticalElement.Propagate(this);
            
            // After propagation through optical elements, 
            // we need to ensure the next segment in the chain 
            // has the updated total distance
            if (totalDistance < originalDistance + segmentLength) {
                // If the totalDistance wasn't properly updated by the optical element,
                // we'll ensure it's at least the original distance plus this segment
                totalDistance = originalDistance + segmentLength;
            }
        }
    }
    
    private void UpdateVisuals() {
        if (!lineRenderer) return;
        
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
    }
}