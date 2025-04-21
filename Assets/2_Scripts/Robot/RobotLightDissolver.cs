using System.Collections.Generic;
using UnityEngine;
using VInspector;


public class RobotLightDissolver : MonoBehaviour
{

    [Header("Shape Settings")]
    public float radius = 1.7f;
    public bool animateRadius = true;
    public float animationSpeed = 1.0f;
    public float minRadius = 1.7f;
    public float maxRadius = 1.75f;
    
    
    [Header("Raycast Movement")]
    public bool enableRaycastMovement = true;
    public float movementSpeed = 15.0f;
    public float maxRaycastDistance = 10.0f;
    public LayerMask raycastLayers;
    
    
    [Header("Affected Renderers")]
    [Tooltip("List of renderers to affect. Only these renderers will be affected.")]
    public List<Renderer> targetRenderers = new List<Renderer>();
    

    
    private RobotCompanion _robot;
    private Vector3 _targetPosition;
    private bool _isMoving;
    private bool MoveToTarget => _robot.PlayerIsAiming && _robot.IsOn() && _robot.CurrentState != RobotState.Sitting;
    
    
    // Static variables for managing multiple interactors
    private const int MaxInteractors = 20;
    private static readonly List<RobotLightDissolver> ActiveInteractors = new List<RobotLightDissolver>();
    private readonly List<Renderer> _affectedRenderers = new List<Renderer>();
    private static readonly int PositionID = Shader.PropertyToID("_Position");
    private static readonly int RadiusID = Shader.PropertyToID("_Radius");
    private static readonly int ShapeTypeID = Shader.PropertyToID("_ShapeType");
    private static readonly int InteractorCountID = Shader.PropertyToID("_InteractorCount");
    private static readonly int InteractorPositionsID = Shader.PropertyToID("_ShaderInteractorsPositions");
    private static readonly int InteractorRadiusesID = Shader.PropertyToID("_ShaderInteractorsRadiuses");
    private bool _needsRefresh = true;
    

    private void Awake()
    {
        _robot = GetComponentInParent<RobotCompanion>();
        _targetPosition = transform.position;
    }

    private void Start()
    {
        FindAllDissolvingObjects();
    }

    private void OnEnable()
    {
        TestManager.Instance?.onTestLoaded.AddListener(OnTestLoaded);
        TestManager.Instance?.onTestStartUnloading.AddListener(OnTestStartUnloading);
        
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
        TestManager.Instance?.onTestLoaded.RemoveListener(OnTestLoaded);
        TestManager.Instance?.onTestStartUnloading.RemoveListener(OnTestStartUnloading);
        
        
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
        // Animate radius if enabled
        if (animateRadius)
        {
            float t = (Mathf.Sin(Time.time * animationSpeed) + 1.0f) * 0.5f;
            radius = Mathf.Lerp(minRadius, maxRadius, t);
        }

        // Handle raycast movement if enabled
        if (enableRaycastMovement && _robot)
        {
            HandleRaycastMovement();
        }

        // Move towards target position if needed
        if (_isMoving && MoveToTarget)
        {
            MoveTowardsTarget();
        }
        else
        {
            // move to the original position
            transform.position = Vector3.MoveTowards(
                transform.position, 
                _robot.transform.position, 
                movementSpeed * Time.deltaTime
            );
        }

        // Update single interactor mode for specified renderers
        if (targetRenderers.Count > 0)
        {
            foreach (Renderer rend in targetRenderers)
            {
                if (rend)
                {
                    foreach (Material mat in rend.sharedMaterials)
                    {
                        UpdateSingleInteractorMaterial(mat);
                    }
                }
            }
        }

        // Update all shaders for multi-interactor mode
        UpdateShaders();
        
        // Refresh affected renderers list occasionally
        if (_needsRefresh)
        {
            RefreshAffectedRenderers();
            _needsRefresh = false;
        }
    }
    
    private void HandleRaycastMovement()
    {
        // Get robot's look direction
        Vector3 lookDirection = _robot.GetLookDirection();
        
        // Perform raycast
        Vector3 rayOrigin = _robot.transform.position;
        
        if (Physics.Raycast(rayOrigin, lookDirection, out var hit, maxRaycastDistance, raycastLayers))
        {
            // Hit something, set target to hit point
            _targetPosition = hit.point;
            _isMoving = true;
        }
        else
        {
            // No hit, move maximum distance in look direction
            _targetPosition = rayOrigin + lookDirection * maxRaycastDistance;
            _isMoving = true;
        }
    }
    
    private void MoveTowardsTarget()
    {
        // Move towards the target position
        transform.position = Vector3.MoveTowards(
            transform.position, 
            _targetPosition, 
            movementSpeed * Time.deltaTime
        );
        
        // Check if we've reached the target
        if (Vector3.Distance(transform.position, _targetPosition) < 0.01f)
        {
            _isMoving = false;
        }
    }
    
    private void OnTestLoaded(SOTest test)
    {
        FindAllDissolvingObjects();
        RefreshAffectedRenderers();
    }
    
    private void OnTestStartUnloading(SOTest test)
    {
        ClearAllInteractors();
    }
    
    

    // Update a single material with this interaction's properties
    private void UpdateSingleInteractorMaterial(Material material)
    {
        if (material.shader.name.Contains("Custom/UnifiedDissolve"))
        {
            material.SetVector(PositionID, transform.position);
            
            // Set shape type (0 for sphere)
            material.SetFloat(ShapeTypeID, 0);
            material.SetFloat(RadiusID, radius);
        }
    }

    // Update all affected materials with all active interactors
    private void UpdateShaders()
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
            RobotLightDissolver interactor = ActiveInteractors[i];
            
            positions[i] = interactor.transform.position;
            radiuses[i] = interactor.radius;
        }

        // Only update materials from targetRenderers list
        Material[] materials = GetAllDissolveShaderMaterials();
        
        foreach (Material mat in materials)
        {
            // Set arrays with all interactors data
            mat.SetVectorArray(InteractorPositionsID, positions);
            mat.SetFloatArray(InteractorRadiusesID, radiuses);
            mat.SetInt(InteractorCountID, count);
        }
    }

    // Find all materials using our dissolve shader, only from targetRenderers
    private  Material[] GetAllDissolveShaderMaterials()
    {
        List<Material> materials = new List<Material>();
        
        // Collect materials only from targetRenderers of all active interactors
        foreach (RobotLightDissolver controller in ActiveInteractors)
        {
            if (controller.targetRenderers.Count > 0)
            {
                foreach (Renderer rend in controller.targetRenderers)
                {
                    if (rend != null)
                    {
                        foreach (Material mat in rend.sharedMaterials)
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
        
        return materials.ToArray();
    }

    // Reset all effects
    private void ClearAllInteractors()
    {
        ActiveInteractors.Clear();
        UpdateShaders();
    }
    
    // Find all renderers affected by this controller (only from targetRenderers)
    private void RefreshAffectedRenderers()
    {
        _affectedRenderers.Clear();
        
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
    
    [Button]
    private void FindAllDissolvingObjects()
    {
        targetRenderers.Clear();
        
        // Find all DissolvingObject components in the scene
        DissolvingObject[] dissolvingObjects = FindObjectsByType<DissolvingObject>(FindObjectsSortMode.None);

        
        // Add all renderers from DissolvingObject scripts
        foreach (DissolvingObject obj in dissolvingObjects)
        {
            // Get renderer on the same object
            Renderer objRenderer = obj.rendererToDissolve;
            if (objRenderer != null && !targetRenderers.Contains(objRenderer))
            {
                targetRenderers.Add(objRenderer);
            }
            
            // Also get any renderers assigned directly to the DissolvingObject
            if (obj.rendererToDissolve != null && !targetRenderers.Contains(obj.rendererToDissolve))
            {
                targetRenderers.Add(obj.rendererToDissolve);
            }
            
            // And get any renderers in the child objects if enabled
            if (obj.includeChildRenderers)
            {
                Renderer[] childRenderers = obj.GetAllChildRenderers();
                foreach (Renderer childRend in childRenderers)
                {
                    if (!targetRenderers.Contains(childRend))
                    {
                        targetRenderers.Add(childRend);
                    }
                }
            }
        }
        
        // Force refresh of affected renderers
        _needsRefresh = true;
        RefreshAffectedRenderers();
    }
    
    
    
#if UNITY_EDITOR
    
    #region Gizmos
    
    private void OnDrawGizmosSelected()
    {
        DrawShapeGizmo();
        DrawConnectionGizmos();
        DrawRaycastGizmo();
    }
    
    private void DrawShapeGizmo()
    {
        DrawSphereGizmo();
        
        // If animating sphere, also show the max range
        if (animateRadius)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, maxRadius);
        }
    }
    
    private void DrawSphereGizmo()
    {
        // Draw wireframe for better visibility
        Gizmos.color = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.1f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
    
    private void DrawConnectionGizmos()
    {
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
    
    private void DrawRaycastGizmo()
    {
        if (enableRaycastMovement && _robot != null)
        {
            // Draw ray from robot in look direction
            Vector3 startPos = _robot.transform.position;
            Vector3 direction = _robot.GetLookDirection();
            
            Gizmos.color = Color.red;
            Gizmos.DrawRay(startPos, direction * maxRaycastDistance);
            
            // Draw target position
            if (_isMoving)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(_targetPosition, 0.2f);
            }
        }
    }
    
    #endregion
    
#endif
}