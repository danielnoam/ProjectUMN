using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
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
    [SerializeField, Range(0f, 8f)] private float ambientIntensity = 1f;
    [SerializeField] private DefaultReflectionMode reflectionMode = DefaultReflectionMode.Skybox;
    
    [Header("Fog")]
    [SerializeField] private bool useFog = false;
    [SerializeField] private FogMode fogMode = FogMode.Exponential;
    [SerializeField] private  Color fogColor = Color.white;
    [SerializeField, Range(0f, 1f), HideIf("fogMode", FogMode.Linear)] private float fogDensity = 0;[EndIf]
    [SerializeField, ShowIf("fogMode", FogMode.Linear)] private float fogStart = 0;[EndIf]
    [SerializeField, ShowIf("fogMode", FogMode.Linear)] private float fogEnd = 300;[EndIf]

    
    
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
    
    public void ApplyLightingSetting()
    {
        RenderSettings.ambientIntensity = ambientIntensity;
        RenderSettings.defaultReflectionMode = reflectionMode;
        RenderSettings.fog = useFog;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogColor = fogColor;
        if (fogMode is FogMode.Exponential or FogMode.ExponentialSquared)
        {
            RenderSettings.fogDensity = fogDensity;
        }
        else if (fogMode == FogMode.Linear)
        {
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
        }
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
    
}
