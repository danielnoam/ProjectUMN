using System;
using UnityEngine;
using UnityEngine.Serialization;
using VInspector;
using System.Collections.Generic;

public class ReflectionProbePositioner : MonoBehaviour
{
    [Header("Reflection Probe Positioner")]
    [SerializeField] private string[] objectsToIgnore = new string[] { "Floor", "LaserGround" };
    [SerializeField] private ReflectionProbe reflectionProbe;
    


    private void OnEnable()
    {
        TestManager.Instance?.onTestLoaded.AddListener(OnTestLoaded);
    }
    
    private void OnDisable()
    {
        TestManager.Instance?.onTestLoaded.RemoveListener(OnTestLoaded);
    }
    
    private void OnTestLoaded(SOTest test)
    {
        RenderProbe();
    }
    
    [Button] private void RenderProbe()
    {
        reflectionProbe?.RenderProbe();
    }

    [Button]
    private void PositionInMiddle()
    {
        // Get the bounds of all objects in the parent
        Bounds bounds = new Bounds();
        bool boundsInitialized = false;

        foreach (var obj in GetComponentsInParent<Renderer>())
        {
            // Skip objects whose names contain any of the strings to ignore
            bool shouldIgnore = false;
            foreach (string ignoreString in objectsToIgnore)
            {
                if (obj.name.Contains(ignoreString))
                {
                    shouldIgnore = true;
                    break;
                }
            }
            
            if (shouldIgnore)
                continue;

            // Initialize or encapsulate bounds
            if (!boundsInitialized)
            {
                bounds = obj.bounds;
                boundsInitialized = true;
            }
            else
            {
                bounds.Encapsulate(obj.bounds);
            }
        }

        if (boundsInitialized)
        {
            // Set the position and size of the reflection probe
            transform.position = bounds.center;
            reflectionProbe.size = bounds.size;
        }
    }
}