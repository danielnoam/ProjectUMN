using System;
using UnityEngine;
using UnityEngine.Rendering;
using VInspector;

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