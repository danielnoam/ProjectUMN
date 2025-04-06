using UnityEngine;
using VInspector;
using System;
using UnityEngine.Rendering;


[Serializable]
public class TestLightSettings {

    [Header("Lighting")]
    [Range(0f, 8f)] public float ambientIntensity = 1f;
    public DefaultReflectionMode reflectionMode = DefaultReflectionMode.Skybox;
    
    [Header("Fog")]
    public bool useFog = false;
    public FogMode fogMode = FogMode.Exponential;
    public Color fogColor = Color.white;
    [HideIf("fogMode", FogMode.Linear), Range(0f, 1f)] public float fogDensity = 0;[EndIf]
    [ShowIf("fogMode", FogMode.Linear)] public float fogStart = 0;[EndIf]
    [ShowIf("fogMode", FogMode.Linear)] public float fogEnd = 300;
    
}


[Serializable]
public class TestEnvironmentSettings
{
    public GameObject environmentPrefab;
    public Material floorMaterial;
    public Vector3 floorScale = new Vector3(2000, 1, 2000);
}


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

    [Header("World")] 
    [SerializeField] private TestEnvironmentSettings environmentSettings;
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
        if (!environmentSettings.environmentPrefab)
        {
            Debug.Log("No prefab set for " + name);
            return null;
        }
        return environmentSettings.environmentPrefab;
    }
    
    public Vector3 GetPlayerSpawnPoint()
    {
        if (!environmentSettings.environmentPrefab)
        {
            Debug.Log("No prefab set for " + name);
            return Vector3.up;
        }

        SpawnPlatform spawnPlatform = environmentSettings.environmentPrefab.GetComponentInChildren<SpawnPlatform>();
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
        return environmentSettings.floorMaterial;
    }
    
    public Vector3 GetFloorScale()
    {
        return environmentSettings.floorScale;
    }
    
    
    public Vector3 GetRobotSpawnPoint()
    {
        if (!environmentSettings.environmentPrefab)
        {
            Debug.Log("No prefab set for " + name);
            return Vector3.up;
        }

        SpawnPlatform spawnPlatform = environmentSettings.environmentPrefab.GetComponentInChildren<SpawnPlatform>();
        if (!spawnPlatform)
        {
            Debug.Log("No TestSpawnPosition in " + name);
            return Vector3.up;
        }


        return spawnPlatform.GetSpawnPosition() + new Vector3(1, 1, 1);
    }

    public bool HasRobot()
    {
        if (!environmentSettings.environmentPrefab)
        {
            Debug.Log("No prefab set for " + name);
            return false;
        }

        RobotCompanion robot = environmentSettings.environmentPrefab.GetComponentInChildren<RobotCompanion>();
        if (!robot)
        {
            return false;
        }

        return true;
    }
    
    
    
    [Button]
    private void TestLighting()
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
