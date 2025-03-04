using UnityEngine;

[CreateAssetMenu(fileName = "Test", menuName = "SO Test/New Test")]
public class SOTest : ScriptableObject
{
    
    [SerializeField] private new string name = "Test";
    [SerializeField, Multiline(5)] private string description = "This is a test";
    [SerializeField, Min(0)] private int timeToLoad = 1;
    [SerializeField, Min(0)] private int timeToUnload = 1;
    [SerializeField] private GameObject prefab;
    
    
    public string GetName()
    {
        return name;
    }
    
    public string GetDescription()
    {
        return description;
    }
    
    public int GetTimeToLoad()
    {
        return timeToLoad;
    }
    
    public int GetTimeToUnload()
    {
        return timeToUnload;
    }
    
    public GameObject GetPrefab()
    {
        if (!prefab)
        {
            Debug.Log("No prefab set for " + name);
            return null;
        }
        return prefab;
    }
    
    public Vector3 GetPlayerSpawnPoint()
    {
        if (!prefab)
        {
            Debug.Log("No prefab set for " + name);
            return Vector3.up;
        }

        SpawnPlatform spawnPlatform = prefab.GetComponentInChildren<SpawnPlatform>();
        if (!spawnPlatform)
        {
            Debug.Log("No TestSpawnPosition in " + name);
            return Vector3.up;
        }


        return spawnPlatform.GetSpawnPosition();
    }
    
    public Vector3 GetRobotSpawnPoint()
    {
        Vector3 offset = new Vector3(0, 1f, 1);
        
        if (!prefab)
        {
            Debug.Log("No prefab set for " + name);
            return Vector3.up;
        }

        SpawnPlatform spawnPlatform = prefab.GetComponentInChildren<SpawnPlatform>();
        if (!spawnPlatform)
        {
            Debug.Log("No TestSpawnPosition in " + name);
            return Vector3.up;
        }


        return spawnPlatform.GetSpawnPosition() + offset;
    }

    public bool HasRobot()
    {
        if (!prefab)
        {
            Debug.Log("No prefab set for " + name);
            return false;
        }

        RobotCompanion robot = prefab.GetComponentInChildren<RobotCompanion>();
        if (!robot)
        {
            return false;
        }

        return true;
    }
    
}
