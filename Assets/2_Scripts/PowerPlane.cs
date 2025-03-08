using UnityEngine;
using System.Collections;
using VInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class PowerPlane : MonoBehaviour
{
    [Header("Plane Points")]
    public Transform startPoint;
    public Transform endPoint;
    
    [Header("Plane Settings")]
    [Range(0.5f, 10f)]
    [Tooltip("Width of the plane")]
    public float planeWidth = 2f;
    
    [Range(0.1f, 5f)]
    [Tooltip("Height/thickness of the plane")]
    public float planeHeight = 0.2f;
    
    [Range(0.1f, 5f)]
    [Tooltip("Time to fully activate or deactivate the plane")]
    public float activationTime = 1.0f;
    
    public Material planeMaterial;
    
    [Header("Runtime")]
    [Tooltip("Toggle to activate/deactivate the plane (works in editor and play mode)")]
    public bool isActive = false;
    
    // Components
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;
    private Mesh _mesh;
    
    private Coroutine _activationCoroutine;
    private float _currentAnimationProgress = 0f; // 0 = fully inactive, 1 = fully active
    private bool _targetState = false; // The state we're animating towards
    private bool _meshNeedsRebuild = true;
    
    private void Awake()
    {
        InitializeComponents();
    }
    
    private void Start()
    {
        // Set initial state without animation
        _currentAnimationProgress = isActive ? 1f : 0f;
        _targetState = isActive;
        UpdateMesh();
        
        // Set collider and renderer state
        UpdateComponentStates();
    }
    
    private void Update()
    {
        // Only update if active and if positions have changed
        if (startPoint != null && endPoint != null && 
           (startPoint.hasChanged || endPoint.hasChanged))
        {
            _meshNeedsRebuild = true;
            UpdateMesh();
            startPoint.hasChanged = false;
            endPoint.hasChanged = false;
        }
    }
    
    private void OnEnable()
    {
        if (_meshRenderer != null)
            _meshRenderer.enabled = true;
            
        _meshNeedsRebuild = true;
    }
    
    private void OnDisable()
    {
        if (_meshRenderer != null)
            _meshRenderer.enabled = false;
    }
    
    private void OnDestroy()
    {
        if (_mesh != null)
        {
            if (Application.isPlaying)
                Destroy(_mesh);
            else
                DestroyImmediate(_mesh);
                
            _mesh = null;
        }
    }
    
    private void InitializeComponents()
    {
        // Get or add MeshFilter
        _meshFilter = GetComponent<MeshFilter>();
        if (_meshFilter == null)
            _meshFilter = gameObject.AddComponent<MeshFilter>();
            
        // Get or add MeshRenderer
        _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshRenderer == null)
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            
        // Get or add MeshCollider
        _meshCollider = GetComponent<MeshCollider>();
        if (_meshCollider == null)
            _meshCollider = gameObject.AddComponent<MeshCollider>();
            
        // Create mesh if needed
        if (_mesh == null)
        {
            _mesh = new Mesh();
            _mesh.name = "PlaneMesh_" + gameObject.name;
            _meshFilter.sharedMesh = _mesh;
        }
        
        // Set material
        if (planeMaterial != null && _meshRenderer != null)
        {
            if (Application.isPlaying)
                _meshRenderer.material = planeMaterial;
            else
                _meshRenderer.sharedMaterial = planeMaterial;
        }
    }
    
    private void UpdateMesh()
    {
        if (startPoint == null || endPoint == null || _mesh == null)
            return;
            
        // Skip mesh generation if not needed
        if (!_meshNeedsRebuild && !Application.isPlaying)
            return;
            
        // Calculate plane parameters
        Vector3 direction = endPoint.position - startPoint.position;
        float fullLength = direction.magnitude;
        
        // If points are too close, don't rebuild the mesh
        if (fullLength < 0.001f)
            return;
            
        direction.Normalize();
        
        // Calculate the actual length based on animation progress
        float currentLength = fullLength * _currentAnimationProgress;
        
        // Calculate start position
        Vector3 startPos = startPoint.position;
        
        // Create local axes for the plane orientation
        Vector3 forward = direction;
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, forward).normalized;
        up = Vector3.Cross(forward, right).normalized;
        
        // Calculate half width and height for vertex positions
        float halfWidth = planeWidth * 0.5f;
        float halfHeight = planeHeight * 0.5f;
        
        // Create vertices
        Vector3[] vertices = new Vector3[8];
        
        // Bottom vertices at start
        vertices[0] = transform.InverseTransformPoint(startPos + right * -halfWidth - up * halfHeight);
        vertices[1] = transform.InverseTransformPoint(startPos + right * halfWidth - up * halfHeight);
        
        // Top vertices at start
        vertices[2] = transform.InverseTransformPoint(startPos + right * -halfWidth + up * halfHeight);
        vertices[3] = transform.InverseTransformPoint(startPos + right * halfWidth + up * halfHeight);
        
        // Bottom vertices at end
        vertices[4] = transform.InverseTransformPoint(startPos + forward * currentLength + right * -halfWidth - up * halfHeight);
        vertices[5] = transform.InverseTransformPoint(startPos + forward * currentLength + right * halfWidth - up * halfHeight);
        
        // Top vertices at end
        vertices[6] = transform.InverseTransformPoint(startPos + forward * currentLength + right * -halfWidth + up * halfHeight);
        vertices[7] = transform.InverseTransformPoint(startPos + forward * currentLength + right * halfWidth + up * halfHeight);
        
        // Define triangles (indices for each face)
        int[] triangles = new int[]
        {
            // Left side
            0, 2, 4,
            2, 6, 4,
            
            // Right side
            1, 5, 3,
            3, 5, 7,
            
            // Bottom
            0, 4, 1,
            1, 4, 5,
            
            // Top
            2, 3, 6,
            3, 7, 6,
            
            // Back
            0, 1, 2,
            1, 3, 2,
            
            // Front
            4, 6, 5,
            5, 6, 7
        };
        
        // Define UVs
        Vector2[] uvs = new Vector2[8];
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(1, 0);
        uvs[2] = new Vector2(0, 1);
        uvs[3] = new Vector2(1, 1);
        uvs[4] = new Vector2(0, 0);
        uvs[5] = new Vector2(1, 0);
        uvs[6] = new Vector2(0, 1);
        uvs[7] = new Vector2(1, 1);
        
        // Clear and set mesh data
        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.uv = uvs;
        
        // Recalculate normals and bounds
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        
        // Update mesh collider safely
        UpdateCollider();
        
        _meshNeedsRebuild = false;
    }
    
    private void UpdateCollider()
    {
        if (_meshCollider == null || _mesh == null)
            return;
            
        try
        {
            // Only update collider if plane is active
            if (isActive && _currentAnimationProgress > 0.01f)
            {
                // Check if the mesh is valid for collider
                if (_mesh.vertexCount > 0)
                {
                    // Disable temporarily - helps avoid PhysX errors
                    bool wasEnabled = _meshCollider.enabled;
                    _meshCollider.enabled = false;
                    
                    // Set mesh and re-enable if needed
                    _meshCollider.sharedMesh = null;
                    _meshCollider.sharedMesh = _mesh;
                    
                    // Only try to make convex if we have enough volume
                    Bounds bounds = _mesh.bounds;
                    float minDimension = Mathf.Min(bounds.size.x, bounds.size.y, bounds.size.z);
                    
                    if (minDimension > 0.05f)
                    {
                        _meshCollider.convex = true;
                    }
                    else
                    {
                        _meshCollider.convex = false;
                    }
                    
                    _meshCollider.enabled = wasEnabled && isActive;
                }
            }
            else
            {
                _meshCollider.enabled = false;
            }
        }
        catch (System.Exception e)
        {
            // Log error but don't crash
            Debug.LogWarning("PowerPlane: Error updating collider: " + e.Message);
            _meshCollider.enabled = false;
        }
    }
    
    private void UpdateComponentStates()
    {
        if (_meshRenderer != null)
            _meshRenderer.enabled = isActive;
            
        if (_meshCollider != null)
            _meshCollider.enabled = isActive && _currentAnimationProgress > 0.01f;
    }
    
    [Button]
    public void TogglePlane()
    {
        SetPlaneActive(!isActive);
    }
    
    [Button]
    public void ActivatePlane()
    {
        SetPlaneActive(true);
    }
    
    [Button]
    public void DeactivatePlane()
    {
        SetPlaneActive(false);
    }
    
    // Activate or deactivate the plane
    public void SetPlaneActive(bool active)
    {
        // Check if the GameObject is active before starting a coroutine
        if (!gameObject.activeInHierarchy && Application.isPlaying)
        {
            Debug.LogWarning("Cannot toggle plane on inactive GameObject. Please activate the GameObject first.");
            return;
        }
        
        // Skip if already in desired state
        if (isActive == active && _currentAnimationProgress == (active ? 1f : 0f))
            return;
            
        // Update the target state
        _targetState = active;
        isActive = active;
        
        // Only animate in play mode
        if (Application.isPlaying && gameObject.activeInHierarchy)
        {
            // If there's no active animation, start a new one
            if (_activationCoroutine == null)
            {
                _activationCoroutine = StartCoroutine(AnimatePlane());
            }
            // Otherwise, the existing animation will continue but with the new target state
        }
        else
        {
            // Immediately set the state without animation
            _currentAnimationProgress = active ? 1f : 0f;
            _meshNeedsRebuild = true;
            UpdateMesh();
            UpdateComponentStates();
        }
    }
    
    // Animate the plane activation/deactivation
    private IEnumerator AnimatePlane()
    {
        // Show the renderer during animation
        if (_meshRenderer != null)
            _meshRenderer.enabled = true;
        
        // Get a smaller step size for smoother animation
        float smallStep = 0.05f;
        
        // Animate until we reach the target state
        while ((_targetState && _currentAnimationProgress < 1f) || (!_targetState && _currentAnimationProgress > 0f))
        {
            // Calculate the current target value
            float targetValue = _targetState ? 1f : 0f;
            
            // Calculate step based on the direction we're going, with a limit
            float step = (targetValue - _currentAnimationProgress) > 0 ? 
                Mathf.Min(Time.deltaTime / activationTime, smallStep) : 
                Mathf.Max(-Time.deltaTime / activationTime, -smallStep);
                
            // Update progress
            _currentAnimationProgress = Mathf.Clamp01(_currentAnimationProgress + step);
            
            // Mark mesh for rebuild and update
            _meshNeedsRebuild = true;
            UpdateMesh();
            
            // Wait for next frame
            yield return null;
        }
        
        // Set final state
        UpdateComponentStates();
        
        _activationCoroutine = null;
    }
    
    private void OnValidate()
    {
        // Mark mesh for rebuild
        _meshNeedsRebuild = true;
        
        #if UNITY_EDITOR
        // Queue delayed update to avoid "SendMessage cannot be called during" errors
        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += () => 
            {
                if (this == null || gameObject == null) return;
                
                InitializeComponents();
                
                // Set proper animation progress based on current state
                _currentAnimationProgress = isActive ? 1f : 0f;
                _targetState = isActive;
                
                UpdateMesh();
                UpdateComponentStates();
            };
        }
        #endif
    }
    
    #if UNITY_EDITOR
    // Draw gizmos to show the plane in Scene view
    private void OnDrawGizmos()
    {
        if (startPoint == null || endPoint == null)
            return;
            
        // Draw a line showing the plane path
        Gizmos.color = isActive ? Color.green : Color.red;
        Gizmos.DrawLine(startPoint.position, endPoint.position);
        
        // Draw spheres at start and end
        Gizmos.DrawSphere(startPoint.position, 0.2f);
        Gizmos.DrawSphere(endPoint.position, 0.2f);
        
        // Draw the plane bounds if inactive or in editor
        if (!isActive || !Application.isPlaying)
        {
            // Draw wireframe
            Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan with transparency
            Vector3 direction = endPoint.position - startPoint.position;
            float distance = direction.magnitude;
            Vector3 center = startPoint.position + direction * 0.5f;
            
            // Draw plane bounds
            Matrix4x4 originalMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                center,
                Quaternion.LookRotation(direction),
                Vector3.one
            );
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(planeWidth, planeHeight, distance));
            Gizmos.matrix = originalMatrix;
        }
    }
    #endif
}