using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using VInspector;


public class PlayerDissolveController : MonoBehaviour
{
    [Header("Effect Shape")]
    [Tooltip("Radius of effect for sphere shape")]
    public float radius = 5.0f;
    
    [Header("Animation")]
    [Tooltip("Animation speed")]
    public float animationSpeed = 1.0f;
    
    [Tooltip("Animate the radius over time")]
    public bool animateRadius;
    [Tooltip("Minimum radius value")]
    public float minRadius;
    [Tooltip("Maximum radius value")]
    public float maxRadius = 10.0f;
    
    [Header("Target Options")]
    [Tooltip("Follow another transform")]
    public bool followTarget;

    [Tooltip("Target transform to follow")]
    public Transform targetTransform;

    [Tooltip("Offset from the target position")]
    public Vector3 followOffset = Vector3.zero;

    [Header("Affected Renderers")]
    [Tooltip("List of renderers to affect. Only these renderers will be affected.")]
    public List<Renderer> targetRenderers = new List<Renderer>();
    
    private const int MaxInteractors = 20;
    private static readonly List<PlayerDissolveController> ActiveInteractors = new List<PlayerDissolveController>();
    private readonly List<Renderer> _affectedRenderers = new List<Renderer>();
    private bool _needsRefresh = true;
    private static readonly int PositionID = Shader.PropertyToID("_Position");
    private static readonly int RadiusID = Shader.PropertyToID("_Radius");
    private static readonly int ShapeTypeID = Shader.PropertyToID("_ShapeType");
    private static readonly int InteractorCountID = Shader.PropertyToID("_InteractorCount");
    private static readonly int InteractorPositionsID = Shader.PropertyToID("_ShaderInteractorsPositions");
    private static readonly int InteractorRadiusesID = Shader.PropertyToID("_ShaderInteractorsRadiuses");

    private void OnEnable()
    {
        // Register this interactor
        if (!ActiveInteractors.Contains(this))
        {
            ActiveInteractors.Add(this);
        }
        
        // Update shader with new interactor
        UpdateShaders();
        _needsRefresh = true;
    }

    private void OnDisable()
    {
        // Unregister this interactor
        if (ActiveInteractors.Contains(this))
        {
            ActiveInteractors.Remove(this);
        }
        
        // Update shader without this interactor
        UpdateShaders();
    }

    private void Update()
    {
        // Follow target if enabled
        if (followTarget && targetTransform)
        {
            transform.position = targetTransform.position + followOffset;
        }

        // Animate radius if enabled
        if (animateRadius)
        {
            float t = (Mathf.Sin(Time.time * animationSpeed) + 1.0f) * 0.5f;
            radius = Mathf.Lerp(minRadius, maxRadius, t);
        }

        // Update shaders for all renderers, even when targetRenderers is empty
        UpdateShaders();
        
        // Refresh affected renderers list occasionally
        if (_needsRefresh)
        {
            RefreshAffectedRenderers();
            _needsRefresh = false;
        }
    }

    // Update a single material with this interaction's properties
    private void UpdateSingleInteractorMaterial(Material material)
    {
        if (material.shader.name.Contains("Custom/UnifiedDissolve"))
        {
            material.SetVector(PositionID, transform.position);
            
            // Set shape type to 0 for sphere (only option now)
            material.SetFloat(ShapeTypeID, 0);
            material.SetFloat(RadiusID, radius);
        }
    }

    // Update all affected materials with all active interactors
    private static void UpdateShaders()
    {
        if (ActiveInteractors.Count == 0)
            return;

        // Create arrays to hold all interactor data
        Vector4[] positions = new Vector4[MaxInteractors];
        float[] radiuses = new float[MaxInteractors];

        // Get data from all active interactors
        int count = Mathf.Min(ActiveInteractors.Count, MaxInteractors);
        
        for (int i = 0; i < count; i++)
        {
            PlayerDissolveController interactor = ActiveInteractors[i];
            
            positions[i] = interactor.transform.position;
            radiuses[i] = interactor.radius;
        }

        // Get all materials using our dissolve shader
        Material[] materials = GetAllDissolveShaderMaterials();
        
        foreach (Material mat in materials)
        {
            // Set arrays with all interactors data
            mat.SetVectorArray(InteractorPositionsID, positions);
            mat.SetFloatArray(InteractorRadiusesID, radiuses);
            mat.SetInt(InteractorCountID, count);
        }
    }

    // Find all materials using our dissolve shader
    private static Material[] GetAllDissolveShaderMaterials()
    {
        List<Material> materials = new List<Material>();
        
        // First try to collect materials from targetRenderers of all active interactors
        foreach (PlayerDissolveController controller in ActiveInteractors)
        {
            if (controller.targetRenderers.Count > 0)
            {
                foreach (Renderer renderer in controller.targetRenderers)
                {
                    if (renderer)
                    {
                        foreach (Material mat in renderer.sharedMaterials)
                        {
                            if (mat.shader.name.Contains("Custom/UnifiedDissolve") && !materials.Contains(mat))
                            {
                                materials.Add(mat);
                            }
                        }
                    }
                }
            }
        }
        
        // If no materials were found from targetRenderers, find all materials in the scene using the shader
        if (materials.Count == 0)
        {
            Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            
            foreach (Renderer renderer in allRenderers)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat.shader.name.Contains("Custom/UnifiedDissolve") && !materials.Contains(mat))
                    {
                        materials.Add(mat);
                    }
                }
            }
        }
        
        return materials.ToArray();
    }

    // Reset all effects
    public static void ClearAllInteractors()
    {
        ActiveInteractors.Clear();
        UpdateShaders();
    }
    
    // Find all renderers affected by this controller
    private void RefreshAffectedRenderers()
    {
        _affectedRenderers.Clear();
        
        // If targetRenderers list is empty, find all renderers using the shader
        if (targetRenderers.Count == 0)
        {
            Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            
            foreach (Renderer rend in allRenderers)
            {
                bool hasDissolveShader = false;
                
                foreach (Material mat in rend.sharedMaterials)
                {
                    if (mat.shader.name.Contains("Custom/UnifiedDissolve"))
                    {
                        hasDissolveShader = true;
                        break;
                    }
                }
                
                if (hasDissolveShader)
                {
                    _affectedRenderers.Add(rend);
                }
            }
        }
        else
        {
            // Only use renderers from the targetRenderers list
            foreach (Renderer rend in targetRenderers)
            {
                if (rend)
                {
                    bool hasDissolveShader = false;
                    
                    foreach (Material mat in rend.sharedMaterials)
                    {
                        if (mat.shader.name.Contains("Custom/UnifiedDissolve"))
                        {
                            hasDissolveShader = true;
                            break;
                        }
                    }
                    
                    if (hasDissolveShader)
                    {
                        _affectedRenderers.Add(rend);
                    }
                }
            }
        }
    }
    
    
#if UNITY_EDITOR
    
    #region Gizmos
    
    private void OnDrawGizmosSelected()
    {
        // Draw wireframe for better visibility
        Gizmos.color = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.1f);
        Gizmos.DrawWireSphere(transform.position, radius);
        
        // If animating sphere, also show the max range
        if (animateRadius)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, maxRadius);
        }
        
        // Refresh renderers list if empty
        if (_affectedRenderers.Count == 0)
        {
            RefreshAffectedRenderers();
        }
        
        // Draw connections to affected renderers
        Gizmos.color = Color.green;
        
        foreach (Renderer rend in _affectedRenderers)
        {
            if (rend)
            {
                // Get renderer center position
                Vector3 rendererCenter = rend.bounds.center;
                
                // Draw connection line
                Gizmos.DrawLine(transform.position, rendererCenter);
                
                // Draw small sphere at renderer position
                Gizmos.DrawSphere(rendererCenter, 0.1f);
                
                // Draw distance label
                float distance = Vector3.Distance(transform.position, rendererCenter);
                    
                // Only draw labels in scene view, not in game view
                UnityEditor.Handles.Label(
                    Vector3.Lerp(transform.position, rendererCenter, 0.5f), 
                    distance.ToString("F1") + "m");
            }
        }
    }

    
    #endregion
#endif
}