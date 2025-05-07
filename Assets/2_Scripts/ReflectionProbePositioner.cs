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
        // Get the bounds of all objects in the scene
        Bounds bounds = new Bounds();
        bool boundsInitialized = false;

        // Find all renderers in the scene
        Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        foreach (var renderer in allRenderers)
        {
            // Check if this renderer or any of its parents have names containing strings to ignore
            bool shouldIgnore = false;
        
            // Check the renderer and all its parent objects
            Transform current = renderer.transform;
            while (current != null)
            {
                // Check against each ignore string
                foreach (string ignoreString in objectsToIgnore)
                {
                    if (current.name.Contains(ignoreString))
                    {
                        shouldIgnore = true;
                        break;
                    }
                }
            
                if (shouldIgnore)
                    break;
                
                // Move up to the parent
                current = current.parent;
            }
        
            // Also skip inactive GameObjects
            if (shouldIgnore || !renderer.gameObject.activeSelf)
                continue;

            // Initialize or encapsulate bounds
            if (!boundsInitialized)
            {
                bounds = renderer.bounds;
                boundsInitialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (boundsInitialized)
        {
            // Set the position and size of the reflection probe
            transform.position = bounds.center;
        
            // Add a small buffer to make sure everything is captured
            Vector3 sizeWithBuffer = bounds.size * 1.1f;
            reflectionProbe.size = sizeWithBuffer;
        }
        else
        {
            Debug.LogWarning("No valid renderers found for the reflection probe bounds calculation.");
        }
    }
}