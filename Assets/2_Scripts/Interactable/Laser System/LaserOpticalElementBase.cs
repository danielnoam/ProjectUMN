using System.Collections.Generic;
using UnityEngine;

public abstract class LaserOpticalElementBase : MonoBehaviour
{
    protected readonly List<LaserBeamPair> _laserBeamPairs = new List<LaserBeamPair>();
    public List<LaserBeamPair> LaserBeamPairs => _laserBeamPairs;

    public virtual void RegisterLaserBeam(LaserBeam laserBeam) {
        // Check if we already have a pair for this incoming beam
        LaserBeamPair existingPair = GetPairFromIncomingBeam(laserBeam);
        
        if (existingPair != null) {
            // We already have this beam registered, so update properties
            // but don't create a new outgoing beam
            existingPair.outgoing.maxTotalDistance = laserBeam.maxTotalDistance;
            existingPair.outgoing.totalDistance = laserBeam.totalDistance;
            
            // Update visual properties in case they've changed
            existingPair.outgoing.SetBeamProperties(
                laserBeam.lineRenderer.startWidth, 
                laserBeam.lineRenderer.startColor, 
                laserBeam.lineRenderer.material
            );
            return;
        }
        
        // Create new outgoing beam since this is a new registration
        LaserBeam outgoingLaserBeam = Instantiate(laserBeam.prefab, transform);
        if (!outgoingLaserBeam) return;
        
        // Copy beam properties (color, width, material)
        outgoingLaserBeam.SetBeamProperties(
            laserBeam.lineRenderer.startWidth, 
            laserBeam.lineRenderer.startColor, 
            laserBeam.lineRenderer.material
        );
        
        // Share the same maximum total distance
        outgoingLaserBeam.maxTotalDistance = laserBeam.maxTotalDistance;
        
        // Inherit the accumulated distance from the incoming beam
        outgoingLaserBeam.totalDistance = laserBeam.totalDistance;
        
        _laserBeamPairs.Add(new LaserBeamPair(laserBeam, outgoingLaserBeam));
    }
    
    public virtual void UnregisterLaserBeam(LaserBeam laserBeam) {
        var pair = GetPairFromIncomingBeam(laserBeam);
        
        if (pair == null) return; // Nothing to unregister

        if (pair.outgoing.HitOpticalElement) {
            pair.outgoing.HitOpticalElement.UnregisterLaserBeam(pair.outgoing);
        }

        _laserBeamPairs.Remove(pair);
        Destroy(pair.outgoing.gameObject);
    }

    public virtual void UpdateMaxDistance(LaserBeam laserBeam, float maxTotalDistance) {
        var pair = GetPairFromIncomingBeam(laserBeam);
        if (pair != null) {
            pair.outgoing.maxTotalDistance = maxTotalDistance;
        
            // Propagate update to any elements hit by the outgoing beam
            if (pair.outgoing.HitOpticalElement) {
                pair.outgoing.HitOpticalElement.UpdateMaxDistance(pair.outgoing, maxTotalDistance);
            }
        }
    }

    public virtual void CleanupConnectedBeams()
    {
        // Make a copy of the list to avoid modification during enumeration
        var beamPairsCopy = new List<LaserBeamPair>(_laserBeamPairs);
    
        foreach (var pair in beamPairsCopy)
        {
            // Recursively clean up any downstream beams first
            if (pair.outgoing.HitOpticalElement)
            {
                pair.outgoing.HitOpticalElement.CleanupConnectedBeams();
            }
        
            // Clear PowerPoint connection if any
            if (pair.outgoing.HitPowerPoint)
            {
                pair.outgoing.HitPowerPoint = null;
            }
        
            // Remove from the list and destroy the outgoing beam
            _laserBeamPairs.Remove(pair);
            Destroy(pair.outgoing.gameObject);
        }
    }
    
    protected LaserBeamPair GetPairFromIncomingBeam(LaserBeam laserBeam) => 
        _laserBeamPairs.Find(x => x.incoming == laserBeam);
        
    public abstract void Propagate(LaserBeam laserBeam);
}