using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LaserMirror : LaserOpticalElementBase
{
    private readonly List<LaserBeamPair> _laserBeamPairs = new List<LaserBeamPair>();

    public override void RegisterLaserBeam(LaserBeam laserBeam) {
        // Check if we already have a pair for this incoming beam
        LaserBeamPair existingPair = GetPairFromIncomingBeam(laserBeam);
        
        if (existingPair != null) {
            // We already have this beam registered, so update properties
            // but don't create a new outgoing beam
            existingPair.outgoing.maxTotalDistance = laserBeam.maxTotalDistance;
            existingPair.outgoing.totalDistance = laserBeam.totalDistance;
            
            // Update visual properties in case they've changed
            existingPair.outgoing.SetBeamProperties(
                laserBeam._lineRenderer.startWidth, 
                laserBeam._lineRenderer.startColor, 
                laserBeam._lineRenderer.material
            );
            return;
        }
        
        // Create new outgoing beam since this is a new registration
        LaserBeam outgoingLaserBeam = GameObject.Instantiate(laserBeam.prefab, transform);
        
        // Copy beam properties (color, width, material)
        outgoingLaserBeam.SetBeamProperties(
            laserBeam._lineRenderer.startWidth, 
            laserBeam._lineRenderer.startColor, 
            laserBeam._lineRenderer.material
        );
        
        // Share the same maximum total distance
        outgoingLaserBeam.maxTotalDistance = laserBeam.maxTotalDistance;
        
        // Inherit the accumulated distance from the incoming beam
        outgoingLaserBeam.totalDistance = laserBeam.totalDistance;
        
        _laserBeamPairs.Add(new LaserBeamPair(laserBeam, outgoingLaserBeam));
    }
    
    public override void UnregisterLaserBeam(LaserBeam laserBeam) {
        var pair = GetPairFromIncomingBeam(laserBeam);
        
        if (pair == null) return; // Nothing to unregister

        if (pair.outgoing.LaserOpticalElementBaseThatTheBeamHit != null) {
            pair.outgoing.LaserOpticalElementBaseThatTheBeamHit.UnregisterLaserBeam(pair.outgoing);
        }

        _laserBeamPairs.Remove(pair);
        GameObject.Destroy(pair.outgoing.gameObject);
    }

    public override void Propagate(LaserBeam laserBeam) {
        var pair = GetPairFromIncomingBeam(laserBeam);
        
        if (pair == null) return; // Safety check
        
        // Update the outgoing beam's totalDistance to match the incoming beam
        // This ensures the accumulated distance is passed along
        pair.outgoing.totalDistance = pair.incoming.totalDistance;
        
        Vector3 outgoingDirection = Vector3.Reflect(pair.incoming.Direction, pair.incoming.hitNormal);
        pair.outgoing.Propagate(pair.incoming.endPosition, outgoingDirection);
    }
    
    public override void UpdateMaxDistance(LaserBeam laserBeam, float maxTotalDistance) {
        var pair = GetPairFromIncomingBeam(laserBeam);
        if (pair != null) {
            pair.outgoing.maxTotalDistance = maxTotalDistance;
        
            // Propagate update to any elements hit by the outgoing beam
            if (pair.outgoing.LaserOpticalElementBaseThatTheBeamHit != null) {
                pair.outgoing.LaserOpticalElementBaseThatTheBeamHit.UpdateMaxDistance(pair.outgoing, maxTotalDistance);
            }
        }
    }

    private LaserBeamPair GetPairFromIncomingBeam(LaserBeam laserBeam) => _laserBeamPairs.Find(x => x.incoming == laserBeam);
}