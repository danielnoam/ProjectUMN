using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;
using VInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PowerPlane : MonoBehaviour
{
    [Header("Plane Points")]
    public Transform startPoint;
    public Transform endPoint;
    
    [Header("Plane Settings")]
    public float planeWidth = 2f;
    public float planeHeight = 0.2f;
    public float activationTime = 1.0f;
    public Material planeMaterial;
    
    [Header("Orientation Settings")]
    [Tooltip("When enabled, the plane will be oriented vertically like a wall")]
    public bool isVertical = false;
    
    [Header("Runtime")]
    [Tooltip("Toggle to activate/deactivate the plane (works in editor and play mode)")]
    public bool isActive = false;
    
    // Components
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;
    private Mesh _planeMesh;
    
    private Coroutine _activationCoroutine;
    private float _currentAnimationProgress = 0f; // 0 = fully inactive, 1 = fully active
    private bool _targetState = false; // The state we're animating towards
    
    private void Awake()
    {
        // Get or create components
        SetupComponents();
        
        // Create new mesh
        CreateNewMesh();
        
        // Set material
        if (planeMaterial != null)
            _meshRenderer.material = planeMaterial;
            
        // Initialize mesh collider
        _meshCollider.sharedMesh = _planeMesh;
        _meshCollider.convex = true;
    }
    
    private void SetupComponents()
    {
        _meshFilter = GetComponent<MeshFilter>();
        if (_meshFilter == null)
            _meshFilter = gameObject.AddComponent<MeshFilter>();
            
        _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshRenderer == null)
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            
        _meshCollider = GetComponent<MeshCollider>();
        if (_meshCollider == null)
            _meshCollider = gameObject.AddComponent<MeshCollider>();
    }
    
    private void CreateNewMesh()
    {
        // Clean up old mesh if it exists
        if (_planeMesh != null)
        {
            if (Application.isPlaying)
                Destroy(_planeMesh);
            else
                DestroyImmediate(_planeMesh);
        }
        
        // Create new mesh
        _planeMesh = new Mesh();
        _planeMesh.name = "PlaneMesh_" + gameObject.name;
        _meshFilter.mesh = _planeMesh;
    }
    
    private void Start()
    {
        // Set initial state
        CreatePlaneMesh(1.0f); // Create full-size mesh first
        SetPlaneActive(isActive, false);
        
        // Initialize animation progress based on current state
        _currentAnimationProgress = isActive ? 1f : 0f;
        _targetState = isActive;
    }
    
    // Update the plane if points are moved at runtime
    private void Update()
    {
        // Only update if active and if positions have changed
        if (isActive && startPoint != null && endPoint != null && 
           (startPoint.hasChanged || endPoint.hasChanged))
        {
            CreatePlaneMesh(1.0f);
            startPoint.hasChanged = false;
            endPoint.hasChanged = false;
        }
    }
    
    private void OnDestroy()
    {
        // Clean up mesh when the component is destroyed
        if (_planeMesh != null)
        {
            if (Application.isPlaying)
                Destroy(_planeMesh);
            else
                DestroyImmediate(_planeMesh);
        }
    }
    
    // Create plane mesh based on points
    private void CreatePlaneMesh(float lengthMultiplier = 1.0f, float startOffset = 0.0f)
    {
        if (startPoint == null || endPoint == null || _planeMesh == null)
            return;
            
        // Calculate plane parameters
        Vector3 direction = endPoint.position - startPoint.position;
        float fullLength = direction.magnitude;
        direction.Normalize();
        
        // Calculate the actual length based on multiplier
        float currentLength = fullLength * lengthMultiplier;
        
        // Calculate start position with offset
        Vector3 startPos = startPoint.position + direction * (fullLength * startOffset);
        
        // Create local axes for the plane orientation
        Vector3 forward = direction;
        
        Vector3 up, right;
        
        if (isVertical)
        {
            // For a vertical plane, the "up" direction is actually the world up
            up = Vector3.up;
            right = Vector3.Cross(up, forward).normalized;
        }
        else
        {
            // For a horizontal plane, the "up" direction is perpendicular to the plane direction
            up = Vector3.up;
            right = Vector3.Cross(up, forward).normalized;
            up = Vector3.Cross(forward, right).normalized;
        }
        
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
        _planeMesh.Clear();
        _planeMesh.vertices = vertices;
        _planeMesh.triangles = triangles;
        _planeMesh.uv = uvs;
        
        // Recalculate normals and bounds
        _planeMesh.RecalculateNormals();
        _planeMesh.RecalculateBounds();
        
        // Update mesh collider
        if (_meshCollider != null)
        {
            _meshCollider.sharedMesh = null; // Necessary to force update
            _meshCollider.sharedMesh = _planeMesh;
        }
    }
    
    [Button]
    public void TogglePlane()
    {
        // Check if the GameObject is active before starting a coroutine
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Cannot toggle plane on inactive GameObject. Please activate the GameObject first.");
            return;
        }
        
        SetPlaneActive(!isActive);
    }
    
    [Button]
    public void ActivatePlane()
    {
        if (isActive) return;
        
        // Check if the GameObject is active before starting a coroutine
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Cannot activate plane on inactive GameObject. Please activate the GameObject first.");
            return;
        }
        
        SetPlaneActive(true);
    }
    
    [Button]
    public void DeactivatePlane()
    {
        if (!isActive) return;
        
        // Check if the GameObject is active before starting a coroutine
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Cannot deactivate plane on inactive GameObject. Please activate the GameObject first.");
            return;
        }
        
        SetPlaneActive(false);
    }
    
    [Button]
    public void ToggleOrientation()
    {
        isVertical = !isVertical;
        if (isActive)
        {
            CreatePlaneMesh(1.0f);
        }
    }
    
    // Activate or deactivate the plane
    private void SetPlaneActive(bool active, bool animate = true)
    {
        // Update the target state
        _targetState = active;
        isActive = active;
        
        // Only animate in play mode and if animation is requested
        if (animate && Application.isPlaying && gameObject.activeInHierarchy)
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
            
            if (_meshRenderer != null)
                _meshRenderer.enabled = active;
                
            if (_meshCollider != null)
                _meshCollider.enabled = active;
                
            // Update mesh to show the correct state
            CreatePlaneMesh(active ? 1.0f : 0.0f);
        }
    }
    
    // Animate the plane activation/deactivation
    private IEnumerator AnimatePlane()
    {
        // Show the renderer during animation
        _meshRenderer.enabled = true;
        
        // Keep collider active during animation
        _meshCollider.enabled = true;
        
        // Animate until we reach the target state
        while ((_targetState && _currentAnimationProgress < 1f) || (!_targetState && _currentAnimationProgress > 0f))
        {
            // Calculate the current target value
            float targetValue = _targetState ? 1f : 0f;
            
            // Calculate step based on the direction we're going
            float step = (targetValue - _currentAnimationProgress) > 0 ? 
                Time.deltaTime / activationTime : 
                -Time.deltaTime / activationTime;
                
            // Update progress
            _currentAnimationProgress = Mathf.Clamp01(_currentAnimationProgress + step);
            
            // Update mesh
            CreatePlaneMesh(_currentAnimationProgress);
            
            yield return null;
            
            // Check if target state changed during yield
            // No need for special handling as we'll update towards the new target state
        }
        
        // Set final state
        if (_targetState)
        {
            // Final full-size mesh
            CreatePlaneMesh(1.0f);
        }
        else
        {
            // Disable rendering and collider when fully deactivated
            _meshRenderer.enabled = false;
            _meshCollider.enabled = false;
        }
        
        _activationCoroutine = null;
    }
    
    #if UNITY_EDITOR
    // Also update in edit mode for easier debugging
    private void OnValidate()
    {
        // During OnValidate we just flag that we need to update
        // To avoid the "SendMessage cannot be called during..." error, we'll delay the mesh creation
        EditorApplication.delayCall += () => 
        {
            // This may be called even after the object is destroyed
            if (this == null) return;
            
            // Ensure we have required components
            if (gameObject == null) return;
            
            if (!Application.isPlaying)
            {
                // Initialize references for editor
                SetupComponents();
                
                // Create mesh if it doesn't exist
                if (_planeMesh == null)
                {
                    _planeMesh = new Mesh();
                    _planeMesh.name = "PlaneMesh_" + gameObject.name;
                    
                    // We use delayCall, so setting sharedMesh should be safe now
                    if (_meshFilter != null)
                        _meshFilter.sharedMesh = _planeMesh;
                }
                
                // Update the mesh
                if (_planeMesh != null && startPoint != null && endPoint != null)
                {
                    // In editor mode, always set to final state without animation
                    _currentAnimationProgress = isActive ? 1f : 0f;
                    _targetState = isActive;
                    
                    CreatePlaneMesh(_currentAnimationProgress);
                }
                
                // Visual representation in editor
                if (_meshRenderer != null)
                {
                    _meshRenderer.enabled = isActive;
                    
                    if (planeMaterial != null)
                        _meshRenderer.sharedMaterial = planeMaterial;
                    
                    // Make sure the mesh collider has the current mesh
                    if (_meshCollider != null && _planeMesh != null)
                    {
                        _meshCollider.sharedMesh = _planeMesh;
                        _meshCollider.enabled = isActive;
                    }
                }
            }
        };
    }
    
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
            
            // Choose dimensions based on orientation
            Vector3 size = new Vector3(planeWidth, planeHeight, distance);
            
            // Draw the plane bounds
            Matrix4x4 originalMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                center,
                Quaternion.LookRotation(direction),
                Vector3.one
            );
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.matrix = originalMatrix;
        }
    }
    #endif
}