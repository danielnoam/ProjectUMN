using UnityEngine;

public abstract class LaserOpticalElementBase : MonoBehaviour
{
    public abstract void RegisterLaserBeam(LaserBeam laserBeam);

    public abstract void UnregisterLaserBeam(LaserBeam laserBeam);

    public abstract void Propagate(LaserBeam laserBeam);
    
    public abstract void UpdateMaxDistance(LaserBeam laserBeam, float maxTotalDistance);
}