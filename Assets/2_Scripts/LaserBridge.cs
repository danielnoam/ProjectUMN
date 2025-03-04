using UnityEngine;
using System.Collections;
using VInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LaserBridge : MonoBehaviour
{
    [Header("Bridge Points")]
    public Transform startPoint;
    public Transform endPoint;
    
    [Header("Bridge Settings")]
    public float bridgeWidth = 2f;
    public float bridgeHeight = 0.2f;
    public float activationTime = 1.0f;
    public Material bridgeMaterial;
    
    [Header("Runtime")]
    [Tooltip("Toggle to activate/deactivate the bridge (works in editor and play mode)")]
    public bool isActive = false;
    
    // Components
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;
    private Mesh _bridgeMesh;
    
    private Coroutine _activationCoroutine;
    private float _currentAnimationProgress = 0f; // 0 = fully inactive, 1 = fully active
    private bool _targetState = false; // The state we're animating towards
    
    private void Awake()
    {
        // Create/get components
        _meshFilter = GetComponent<MeshFilter>();
        if (_meshFilter == null)
            _meshFilter = gameObject.AddComponent<MeshFilter>();
            
        _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshRenderer == null)
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            
        _meshCollider = GetComponent<MeshCollider>();
        if (_meshCollider == null)
            _meshCollider = gameObject.AddComponent<MeshCollider>();
        
        // Create new mesh
        _bridgeMesh = new Mesh();
        _bridgeMesh.name = "BridgeMesh";
        _meshFilter.mesh = _bridgeMesh;
        
        // Set material
        if (bridgeMaterial != null)
            _meshRenderer.material = bridgeMaterial;
            
        // Initialize mesh collider
        _meshCollider.sharedMesh = _bridgeMesh;
        _meshCollider.convex = true;
    }
    
    private void Start()
    {
        // Set initial state
        CreateBridgeMesh(1.0f); // Create full-size mesh first
        SetBridgeActive(isActive, false);
        
        // Initialize animation progress based on current state
        _currentAnimationProgress = isActive ? 1f : 0f;
        _targetState = isActive;
    }
    
    // Update the bridge if points are moved at runtime
    private void Update()
    {
        // Only update if active and if positions have changed
        if (isActive && startPoint != null && endPoint != null && 
           (startPoint.hasChanged || endPoint.hasChanged))
        {
            CreateBridgeMesh(1.0f);
            startPoint.hasChanged = false;
            endPoint.hasChanged = false;
        }
    }
    
    // Create bridge mesh based on points
    private void CreateBridgeMesh(float lengthMultiplier = 1.0f, float startOffset = 0.0f)
    {
        if (startPoint == null || endPoint == null || _bridgeMesh == null)
            return;
            
        // Calculate bridge parameters
        Vector3 direction = endPoint.position - startPoint.position;
        float fullLength = direction.magnitude;
        direction.Normalize();
        
        // Calculate the actual length based on multiplier
        float currentLength = fullLength * lengthMultiplier;
        
        // Calculate start position with offset
        Vector3 startPos = startPoint.position + direction * (fullLength * startOffset);
        
        // Calculate half width and height for vertex positions
        float halfWidth = bridgeWidth * 0.5f;
        float halfHeight = bridgeHeight * 0.5f;
        
        // Create local axes for the bridge orientation
        Vector3 forward = direction;
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, forward).normalized;
        up = Vector3.Cross(forward, right).normalized;
        
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
        _bridgeMesh.Clear();
        _bridgeMesh.vertices = vertices;
        _bridgeMesh.triangles = triangles;
        _bridgeMesh.uv = uvs;
        
        // Recalculate normals and bounds
        _bridgeMesh.RecalculateNormals();
        _bridgeMesh.RecalculateBounds();
        
        // Update mesh collider
        if (_meshCollider != null)
        {
            _meshCollider.sharedMesh = null; // Necessary to force update
            _meshCollider.sharedMesh = _bridgeMesh;
        }
    }
    
    [Button]
    public void ToggleBridge()
    {
        // Check if the GameObject is active before starting a coroutine
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Cannot toggle bridge on inactive GameObject. Please activate the GameObject first.");
            return;
        }
        
        SetBridgeActive(!isActive);
    }
    
    [Button]
    public void ActivateBridge()
    {
        if (isActive) return;
        
        // Check if the GameObject is active before starting a coroutine
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Cannot activate bridge on inactive GameObject. Please activate the GameObject first.");
            return;
        }
        
        SetBridgeActive(true);
    }
    
    [Button]
    public void DeactivateBridge()
    {
        if (!isActive) return;
        
        // Check if the GameObject is active before starting a coroutine
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Cannot deactivate bridge on inactive GameObject. Please activate the GameObject first.");
            return;
        }
        
        SetBridgeActive(false);
    }
    
    // Activate or deactivate the bridge
    private void SetBridgeActive(bool active, bool animate = true)
    {
        // Update the target state
        _targetState = active;
        isActive = active;
        
        if (animate && gameObject.activeInHierarchy)
        {
            // If there's no active animation, start a new one
            if (_activationCoroutine == null)
            {
                _activationCoroutine = StartCoroutine(AnimateBridge());
            }
            // Otherwise, the existing animation will continue but with the new target state
        }
        else
        {
            // Immediately set the state
            _currentAnimationProgress = active ? 1f : 0f;
            
            if (_meshRenderer != null)
                _meshRenderer.enabled = active;
                
            if (_meshCollider != null)
                _meshCollider.enabled = active;
                
            // Update mesh to show the correct state
            CreateBridgeMesh(active ? 1.0f : 0.0f);
        }
    }
    
    // Animate the bridge activation/deactivation
    private IEnumerator AnimateBridge()
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
            CreateBridgeMesh(_currentAnimationProgress);
            
            yield return null;
            
            // Check if target state changed during yield
            // No need for special handling as we'll update towards the new target state
        }
        
        // Set final state
        if (_targetState)
        {
            // Final full-size mesh
            CreateBridgeMesh(1.0f);
        }
        else
        {
            // Disable rendering and collider when fully deactivated
            _meshRenderer.enabled = false;
            _meshCollider.enabled = false;
        }
        
        _activationCoroutine = null;
    }
    
    // Also update in edit mode for easier debugging
    #if UNITY_EDITOR
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
                if (GetComponent<MeshFilter>() == null)
                    gameObject.AddComponent<MeshFilter>();
                    
                if (GetComponent<MeshRenderer>() == null)
                    gameObject.AddComponent<MeshRenderer>();
                    
                if (GetComponent<MeshCollider>() == null)
                    gameObject.AddComponent<MeshCollider>();
                
                // Initialize references for editor
                _meshFilter = GetComponent<MeshFilter>();
                _meshRenderer = GetComponent<MeshRenderer>();
                _meshCollider = GetComponent<MeshCollider>();
                
                // Create mesh if it doesn't exist
                if (_bridgeMesh == null)
                {
                    _bridgeMesh = new Mesh();
                    _bridgeMesh.name = "BridgeMesh";
                    
                    // We use delayCall, so setting sharedMesh should be safe now
                    if (_meshFilter != null)
                        _meshFilter.sharedMesh = _bridgeMesh;
                }
                
                // Update the mesh
                if (_bridgeMesh != null && startPoint != null && endPoint != null)
                {
                    // Update current animation progress based on isActive for editor
                    _currentAnimationProgress = isActive ? 1f : 0f;
                    _targetState = isActive;
                    
                    CreateBridgeMesh(isActive ? 1.0f : 0.0f);
                }
                
                // Visual representation in editor
                if (_meshRenderer != null)
                {
                    _meshRenderer.enabled = isActive;
                    
                    if (bridgeMaterial != null)
                        _meshRenderer.sharedMaterial = bridgeMaterial;
                    
                    // Make sure the mesh collider has the current mesh
                    if (_meshCollider != null && _bridgeMesh != null)
                    {
                        _meshCollider.sharedMesh = _bridgeMesh;
                        _meshCollider.enabled = isActive;
                    }
                }
            }
        };
    }
    
    // Draw gizmos to show the bridge in Scene view
    private void OnDrawGizmos()
    {
        if (startPoint == null || endPoint == null)
            return;
            
        // Draw a line showing the bridge path
        Gizmos.color = isActive ? Color.green : Color.red;
        Gizmos.DrawLine(startPoint.position, endPoint.position);
        
        // Draw spheres at start and end
        Gizmos.DrawSphere(startPoint.position, 0.2f);
        Gizmos.DrawSphere(endPoint.position, 0.2f);
        
        // Draw the bridge bounds if active
        if (isActive)
        {
            // Draw wireframe of the bridge
            Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan with transparency
            Vector3 direction = endPoint.position - startPoint.position;
            float distance = direction.magnitude;
            Vector3 center = startPoint.position + direction * 0.5f;
            Vector3 size = new Vector3(bridgeWidth, bridgeHeight, distance);
            
            // Draw the bridge bounds
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