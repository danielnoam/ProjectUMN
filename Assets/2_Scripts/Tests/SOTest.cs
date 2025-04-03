using UnityEngine;
using VInspector;






[CreateAssetMenu(fileName = "Test", menuName = "SO Test/New Test")]
public class SOTest : ScriptableObject
{
    
    [Header("Test")]
    [SerializeField] private new string name = "Test";
    [SerializeField, Multiline(1)] private string description = "This is a test";
    [SerializeField, Min(0)] private int timeToLoad = 1;
    [SerializeField, Min(0)] private int timeToUnload = 1;
    
    [Header("Audio")]
    [SerializeField] private SOAudioEvent theme;
    
    [Header("Environment")]
    [SerializeField] private GameObject environmentPrefab;
    [SerializeField] private Material floorMaterial;
    [SerializeField] private TestLightSettings lightSettings;

    
    
    
    
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
        if (TestManager.Instance && TestManager.Instance.DebugMode)
        {
            return 1;
        }
        
        return timeToLoad;
    }
    
    public int GetTimeToUnload()
    {
        if (TestManager.Instance && TestManager.Instance.DebugMode)
        {
            return 1;
        }
        
        return timeToUnload;
    }
    
    public GameObject GetPrefab()
    {
        if (!environmentPrefab)
        {
            Debug.Log("No prefab set for " + name);
            return null;
        }
        return environmentPrefab;
    }
    
    public Vector3 GetPlayerSpawnPoint()
    {
        if (!environmentPrefab)
        {
            Debug.Log("No prefab set for " + name);
            return Vector3.up;
        }

        SpawnPlatform spawnPlatform = environmentPrefab.GetComponentInChildren<SpawnPlatform>();
        if (!spawnPlatform)
        {
            Debug.Log("No TestSpawnPosition in " + name);
            return Vector3.up;
        }


        return spawnPlatform.GetSpawnPosition();
    }
    
    public SOAudioEvent GetTheme()
    {
        return theme;
    }
    
    public  TestLightSettings GetLightSettings()
    {
        return lightSettings;
    }
    
    public Material GetFloorMaterial()
    {
        return floorMaterial;
    }
    
    
    public Vector3 GetRobotSpawnPoint()
    {
        if (!environmentPrefab)
        {
            Debug.Log("No prefab set for " + name);
            return Vector3.up;
        }

        SpawnPlatform spawnPlatform = environmentPrefab.GetComponentInChildren<SpawnPlatform>();
        if (!spawnPlatform)
        {
            Debug.Log("No TestSpawnPosition in " + name);
            return Vector3.up;
        }


        return spawnPlatform.GetSpawnPosition();
    }

    public bool HasRobot()
    {
        if (!environmentPrefab)
        {
            Debug.Log("No prefab set for " + name);
            return false;
        }

        RobotCompanion robot = environmentPrefab.GetComponentInChildren<RobotCompanion>();
        if (!robot)
        {
            return false;
        }

        return true;
    }
    
    
    
    [Button]
    private void ApplyLightingSetting()
    {
        RenderSettings.ambientIntensity = lightSettings.ambientIntensity;
        RenderSettings.defaultReflectionMode = lightSettings.reflectionMode;
        RenderSettings.fog = lightSettings.useFog;
        RenderSettings.fogMode = lightSettings.fogMode;
        RenderSettings.fogColor = lightSettings.fogColor;
        if (lightSettings.fogMode is FogMode.Exponential or FogMode.ExponentialSquared)
        {
            RenderSettings.fogDensity = lightSettings.fogDensity;
        }
        else if (lightSettings.fogMode == FogMode.Linear)
        {
            RenderSettings.fogStartDistance = lightSettings.fogStart;
            RenderSettings.fogEndDistance = lightSettings.fogEnd;
        }
    }
}
