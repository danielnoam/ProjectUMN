using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the dissolve effect for objects using the UnifiedDissolveShader.
/// Provides gizmos for visualizing effect ranges and connections.
/// </summary>
public class DissolveController : MonoBehaviour
{
    // Static list of all active interactors
    private static List<DissolveController> activeInteractors = new List<DissolveController>();
    
    // Maximum number of interactors supported by the shader
    private const int MAX_INTERACTORS = 20;
    
    // Cached shader property IDs for better performance
    private static readonly int PositionID = Shader.PropertyToID("_Position");
    private static readonly int RadiusID = Shader.PropertyToID("_Radius");
    private static readonly int InteractorCountID = Shader.PropertyToID("_InteractorCount");
    private static readonly int InteractorPositionsID = Shader.PropertyToID("_ShaderInteractorsPositions");
    private static readonly int InteractorRadiusesID = Shader.PropertyToID("_ShaderInteractorsRadiuses");
    private static readonly int InteractorBoxBoundsID = Shader.PropertyToID("_ShaderInteractorsBoxBounds");
    private static readonly int InteractorRotationID = Shader.PropertyToID("_ShaderInteractorRotation");

    [Header("Effect Shape")]
    [Tooltip("Type of shape to use for the dissolve effect")]
    public InteractorShape shape = InteractorShape.Sphere;

    [Tooltip("Radius of effect for sphere shape")]
    public float radius = 5.0f;

    [Tooltip("Size of the box (x, y, z dimensions)")]
    public Vector3 boxSize = new Vector3(1.0f, 1.0f, 1.0f);

    [Tooltip("Rotation of the box in degrees")]
    public Vector3 boxRotation = Vector3.zero;

    [Header("Animation")]
    [Tooltip("Animate the radius over time")]
    public bool animateRadius = false;

    [Tooltip("Minimum radius value")]
    public float minRadius = 0.0f;

    [Tooltip("Maximum radius value")]
    public float maxRadius = 10.0f;

    [Tooltip("Animation speed")]
    public float animationSpeed = 1.0f;

    [Header("Target Options")]
    [Tooltip("Follow another transform")]
    public bool followTarget = false;

    [Tooltip("Target transform to follow")]
    public Transform targetTransform;

    [Tooltip("Offset from the target position")]
    public Vector3 followOffset = Vector3.zero;

    [Header("Affected Renderers")]
    [Tooltip("List of renderers to affect. If empty, affects all renderers with the shader.")]
    public List<Renderer> targetRenderers = new List<Renderer>();

    [Header("Gizmo Settings")]
    [Tooltip("Color for the main gizmo")]
    public Color gizmoColor = new Color(0f, 0.8f, 1f, 0.4f);
    
    [Tooltip("Color for the maximum animation range")]
    public Color maxRangeColor = new Color(1f, 0.8f, 0f, 0.2f);
    
    [Tooltip("Color for lines connecting to affected objects")]
    public Color connectionColor = new Color(0.2f, 1f, 0.3f, 0.7f);
    
    [Tooltip("Show connections to affected renderers")]
    public bool showConnections = true;
    
    [Tooltip("Show labels with distances")]
    public bool showLabels = true;

    // Shape types for the interactor
    public enum InteractorShape
    {
        Sphere,
        Box
    }

    // Cache for affected renderers (used for gizmos)
    private List<Renderer> affectedRenderers = new List<Renderer>();
    private bool needsRefresh = true;

    private void OnEnable()
    {
        // Register this interactor
        if (!activeInteractors.Contains(this))
        {
            activeInteractors.Add(this);
        }
        
        // Update shader with new interactor
        UpdateShaders();
        needsRefresh = true;
    }

    private void OnDisable()
    {
        // Unregister this interactor
        if (activeInteractors.Contains(this))
        {
            activeInteractors.Remove(this);
        }
        
        // Update shader without this interactor
        UpdateShaders();
    }

    private void Update()
    {
        // Follow target if enabled
        if (followTarget && targetTransform != null)
        {
            transform.position = targetTransform.position + followOffset;
        }

        // Animate radius if enabled
        if (animateRadius)
        {
            float t = (Mathf.Sin(Time.time * animationSpeed) + 1.0f) * 0.5f;
            radius = Mathf.Lerp(minRadius, maxRadius, t);
        }

        // Update single interactor mode for specified renderers
        if (targetRenderers.Count > 0)
        {
            foreach (Renderer renderer in targetRenderers)
            {
                if (renderer != null)
                {
                    foreach (Material mat in renderer.materials)
                    {
                        UpdateSingleInteractorMaterial(mat);
                    }
                }
            }
        }

        // Update all shaders for multi-interactor mode
        UpdateShaders();
        
        // Refresh affected renderers list occasionally
        if (needsRefresh)
        {
            RefreshAffectedRenderers();
            needsRefresh = false;
        }
    }

    // Update a single material with this interactor's properties
    private void UpdateSingleInteractorMaterial(Material material)
    {
        if (material.shader.name.Contains("Custom/UnifiedDissolve"))
        {
            material.SetVector(PositionID, transform.position);
            material.SetFloat(RadiusID, radius);
        }
    }

    // Update all affected materials with all active interactors
    public static void UpdateShaders()
    {
        if (activeInteractors.Count == 0)
            return;

        // Create arrays to hold all interactor data
        Vector4[] positions = new Vector4[MAX_INTERACTORS];
        float[] radiuses = new float[MAX_INTERACTORS];
        Vector4[] boxBounds = new Vector4[MAX_INTERACTORS];
        Vector4[] rotations = new Vector4[MAX_INTERACTORS];

        // Get data from all active interactors
        int count = Mathf.Min(activeInteractors.Count, MAX_INTERACTORS);
        
        for (int i = 0; i < count; i++)
        {
            DissolveController interactor = activeInteractors[i];
            
            positions[i] = interactor.transform.position;
            radiuses[i] = interactor.radius;
            boxBounds[i] = new Vector4(
                interactor.boxSize.x,
                interactor.boxSize.y,
                interactor.boxSize.z,
                0.0f
            );
            rotations[i] = new Vector4(
                interactor.boxRotation.x,
                interactor.boxRotation.y,
                interactor.boxRotation.z,
                0.0f
            );
        }

        // Update all materials with our shader
        Material[] materials = GetAllDissolveShaderMaterials();
        
        foreach (Material mat in materials)
        {
            // Set arrays with all interactors data
            mat.SetVectorArray(InteractorPositionsID, positions);
            mat.SetFloatArray(InteractorRadiusesID, radiuses);
            mat.SetVectorArray(InteractorBoxBoundsID, boxBounds);
            mat.SetVectorArray(InteractorRotationID, rotations);
            mat.SetInt(InteractorCountID, count);
        }
    }

    // Find all materials using our dissolve shader
    private static Material[] GetAllDissolveShaderMaterials()
    {
        List<Material> materials = new List<Material>();
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat.shader.name.Contains("Custom/UnifiedDissolve"))
                {
                    materials.Add(mat);
                }
            }
        }
        
        return materials.ToArray();
    }

    // Reset all effects
    public static void ClearAllInteractors()
    {
        activeInteractors.Clear();
        UpdateShaders();
    }
    
    // Find all renderers affected by this controller
    private void RefreshAffectedRenderers()
    {
        affectedRenderers.Clear();
        
        // If we have specific target renderers, use those
        if (targetRenderers.Count > 0)
        {
            foreach (Renderer renderer in targetRenderers)
            {
                if (renderer != null)
                {
                    affectedRenderers.Add(renderer);
                }
            }
            return;
        }
        
        // Otherwise find all renderers using our shader
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        
        foreach (Renderer renderer in allRenderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat.shader.name.Contains("Custom/UnifiedDissolve"))
                {
                    affectedRenderers.Add(renderer);
                    break; // Add each renderer only once
                }
            }
        }
    }
    
    #region Gizmos
    
    private void OnDrawGizmos()
    {
        DrawShapeGizmo();
        
        if (showConnections)
        {
            // Draw connections to affected renderers
            DrawConnectionGizmos();
        }
    }
    
    private void DrawShapeGizmo()
    {
        // Draw the shape based on the current shape type
        if (shape == InteractorShape.Sphere)
        {
            DrawSphereGizmo();
        }
        else // Box shape
        {
            DrawBoxGizmo();
        }
        
        // If animating, also show the max range
        if (animateRadius && shape == InteractorShape.Sphere)
        {
            Gizmos.color = maxRangeColor;
            Gizmos.DrawWireSphere(transform.position, maxRadius);
        }
    }
    
    private void DrawSphereGizmo()
    {
        // Draw sphere 
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, radius);
        
        // Draw wireframe for better visibility
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.8f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
    
    private void DrawBoxGizmo()
    {
        Matrix4x4 oldMatrix = Gizmos.matrix;
        
        // Create rotation matrix
        Quaternion rotation = Quaternion.Euler(boxRotation);
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, rotation, Vector3.one);
        Gizmos.matrix = rotationMatrix;
        
        // Draw box
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(Vector3.zero, boxSize * 2); // *2 because size is half-extents
        
        // Draw wireframe for better visibility
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.8f);
        Gizmos.DrawWireCube(Vector3.zero, boxSize * 2);
        
        // Restore original matrix
        Gizmos.matrix = oldMatrix;
    }
    
    private void DrawConnectionGizmos()
    {
        // Refresh renderers list if empty
        if (affectedRenderers.Count == 0)
        {
            RefreshAffectedRenderers();
        }
        
        // Draw connections to affected renderers
        Gizmos.color = connectionColor;
        
        foreach (Renderer renderer in affectedRenderers)
        {
            if (renderer != null)
            {
                // Get renderer center position
                Vector3 rendererCenter = renderer.bounds.center;
                
                // Draw connection line
                Gizmos.DrawLine(transform.position, rendererCenter);
                
                // Draw small sphere at renderer position
                Gizmos.DrawSphere(rendererCenter, 0.1f);
                
                // Draw distance label
                if (showLabels)
                {
                    float distance = Vector3.Distance(transform.position, rendererCenter);
                    
                    // Only draw labels in scene view, not in game view
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(
                        Vector3.Lerp(transform.position, rendererCenter, 0.5f), 
                        distance.ToString("F1") + "m");
                    #endif
                }
            }
        }
    }
    
    #endregion
}