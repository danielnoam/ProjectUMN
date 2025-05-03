using UnityEngine;


public interface ISpawnPoint
{
    bool HasReached { get; set; }
    void SetSpawnPointReached();
    void SetSpawnPointNotReached();
    void SpawnPointUsed();
    public Vector3 GetSpawnPosition();
    public Quaternion GetSpawnRotation();
}