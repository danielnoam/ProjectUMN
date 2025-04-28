using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LaserMirror : LaserOpticalElementBase
{
    public override void Propagate(LaserBeam laserBeam) {
        var pair = GetPairFromIncomingBeam(laserBeam);
        
        if (pair == null) return; // Safety check
        
        // Update the outgoing beam's totalDistance to match the incoming beam
        // This ensures the accumulated distance is passed along
        pair.outgoing.totalDistance = pair.incoming.totalDistance;
        
        Vector3 outgoingDirection = Vector3.Reflect(pair.incoming.Direction, pair.incoming.hitNormal);
        
        // Get the current layerMask from the LaserSource that's responsible for this beam
        LayerMask layerMask = -1; // Default to everything
        if (pair.incoming.gameObject.TryGetComponent<LaserSource>(out var source)) {
            layerMask = source.GetCollisionMask();
        }
        
        pair.outgoing.Propagate(pair.incoming.endPosition, outgoingDirection, layerMask);
    }
}