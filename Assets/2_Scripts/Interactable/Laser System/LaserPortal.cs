using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class LaserPortal : LaserOpticalElementBase {
    public Transform target;

    private BoxCollider _boxCollider;

    private void Awake() {
        _boxCollider = GetComponent<BoxCollider>();
    }

    public override void Propagate(LaserBeam laserBeam) {
        var pair = GetPairFromIncomingBeam(laserBeam);
        
        if (pair == null || !target) return; // Safety check
        
        // Update the outgoing beam's totalDistance to match the incoming beam
        // This ensures the accumulated distance is passed along
        pair.outgoing.totalDistance = pair.incoming.totalDistance;
        
        var localHitPosition = transform.InverseTransformPoint(pair.incoming.endPosition);

        Vector3 targetPosition = target.TransformPoint(localHitPosition);

        // Calculate the target direction
        Vector3 localDirection = transform.InverseTransformDirection(pair.incoming.Direction);
        Vector3 targetDirection = target.TransformDirection(localDirection);

        // We add a small offset to the target position to avoid the beam being stuck in the portal
        if (_boxCollider) {
            targetPosition += targetDirection * _boxCollider.size.z;
        }
        
        // Get the current layerMask from the LaserSource that's responsible for this beam
        LayerMask layerMask = -1; // Default to everything
        if (pair.incoming.gameObject.TryGetComponent<LaserSource>(out var source)) {
            layerMask = source.GetCollisionMask();
        }
        
        pair.outgoing.Propagate(targetPosition, targetDirection, layerMask);
    }
}