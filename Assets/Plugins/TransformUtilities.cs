using System;
using UnityEngine;

/// <summary>
/// Utility functions for Transform operations that extend MonoBehaviour
/// </summary>
public static class TransformUtilities
{
    /// <summary>
    /// Checks if a transform is a child of another transform (direct or indirect)
    /// </summary>
    /// <param name="child">The potential child transform</param>
    /// <param name="parent">The potential parent transform</param>
    /// <returns>True if the transform is a child, false otherwise</returns>
    public static bool IsChildOf(this Transform child, Transform parent)
    {
        if (child == null || parent == null)
            return false;
            
        Transform currentParent = child.parent;
        
        while (currentParent != null)
        {
            if (currentParent == parent)
                return true;
                
            currentParent = currentParent.parent;
        }
        
        return false;
    }
    
    /// <summary>
    /// Gets the full hierarchy path of a transform from root to the transform
    /// </summary>
    /// <param name="transform">The transform to get the path for</param>
    /// <returns>A string representing the full hierarchy path</returns>
    public static string GetHierarchyPath(this Transform transform)
    {
        if (transform == null) return "";
        
        // Build the full path from root to this transform
        string path = transform.name;
        Transform parent = transform.parent;
        
        while (parent)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
}