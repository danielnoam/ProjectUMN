using UnityEngine;
using VInspector;


[ExecuteInEditMode]
[SelectionBase]
public class TubeRenderer : MonoBehaviour
{
    [Header("Tube Settings")]
    [Tooltip("Number of sides around the tube circumference")]
    [SerializeField] private int sides = 8;
    
    [Tooltip("Close the start cap of the tube")]
    [SerializeField] private bool closeStartCap = false;
    
    [Tooltip("Close the end cap of the tube")]
    [SerializeField] private bool closeEndCap = false;
    
    [Tooltip("How the tube's radius is determined along its length")]
    [SerializeField] private RadiusMode radiusMode = RadiusMode.Single;
    
    [Tooltip("Animation curve controlling the radius when using Curve mode")]
    [SerializeField, ShowIf("radiusMode", RadiusMode.Curve)] private AnimationCurve radiusCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);[EndIf]
    
    [Tooltip("Base radius value")]
    [SerializeField, HideIf("radiusMode", RadiusMode.Curve)] private float radiusOne = 1.0f;[EndIf]
    
    [Tooltip("End radius value (used only in StartEnd mode)")]
    [SerializeField, ShowIf("radiusMode", RadiusMode.StartEnd)] private float radiusTwo = 1.0f;[EndIf]
    
    [Tooltip("Array of points defining the tube's path")]
    [SerializeField] private Vector3[] positions;


    
    private enum RadiusMode { Single, StartEnd, Curve }
    private Vector3[] _vertices;
    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private bool _meshNeedsRebuild = true;


    public Material material
    {
        get => _meshRenderer.material;
        set => _meshRenderer.material = value;
    }
    
    public Vector3[] Positions
    {
        get => positions;
        set 
        { 
            positions = value;
            _meshNeedsRebuild = true;
        }
    }

    public bool CloseStartCap
    {
        get => closeStartCap;
        set 
        { 
            closeStartCap = value;
            _meshNeedsRebuild = true;
        }
    }

    public bool CloseEndCap
    {
        get => closeEndCap;
        set 
        { 
            closeEndCap = value;
            _meshNeedsRebuild = true;
        }
    }
    
    private void Awake()
    {
        InitializeComponents();
    }

    private void Reset()
    {
        positions = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1)
        };
    }

    private void OnEnable()
    {
        _meshRenderer.enabled = true;
    }

    private void OnDisable()
    {
        _meshRenderer.enabled = false;
    }
    
    private void OnDestroy()
    {
        if (_mesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_mesh);
            }
            else
            {
                DestroyImmediate(_mesh);
            }
            _mesh = null;
        }
    }

    private void Update()
    {
        GenerateMesh();
    }

    private void OnValidate()
    {
        sides = Mathf.Max(3, sides);
        _meshNeedsRebuild = true;
    }
    
    public void SetPositions(Vector3[] newPositions)
    {
        positions = newPositions;
        _meshNeedsRebuild = true;
        GenerateMesh();
    }
    
    private void InitializeComponents()
    {
        _meshFilter = GetComponent<MeshFilter>();
        if (!_meshFilter)
        {
            _meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        _meshRenderer = GetComponent<MeshRenderer>();
        if (!_meshRenderer)
        {
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        if (!_mesh)
        {
            _mesh = new Mesh();
            _mesh.name = "TubeMesh";
            _meshFilter.mesh = _mesh;
        }
    }

    private void GenerateMesh()
    {
        // If we don't have enough points to create a tube
        if (!_mesh || positions == null || positions.Length <= 1)
        {
            if (_mesh)
            {
                _mesh.Clear();
            }
            return;
        }
        
        // Calculate the number of additional vertices needed for caps
        int capVertices = 0;
        if (closeStartCap) capVertices += 1; // Center vertex for start cap
        if (closeEndCap) capVertices += 1;   // Center vertex for end cap
        
        // Calculate the vertices length based on positions and sides
        var verticesLength = sides * positions.Length + capVertices;
        
        // Check if we need to rebuild arrays
        if (_vertices == null || _vertices.Length != verticesLength || _meshNeedsRebuild)
        {
            _vertices = new Vector3[verticesLength];
            
            // Generate indices and UVs with cap consideration
            var indices = GenerateIndices(positions.Length);
            var uvs = GenerateUVs(positions.Length);
            
            _mesh.Clear();
            _mesh.vertices = _vertices;
            _mesh.triangles = indices;
            _mesh.uv = uvs;
            
            _meshNeedsRebuild = false;
        }

        // Generate the mesh vertices
        var currentVertIndex = 0;
        for (int i = 0; i < positions.Length; i++)
        {
            var circle = CalculateCircle(i);
            foreach (var vertex in circle)
            {
                _vertices[currentVertIndex++] = vertex;
            }
        }

        // Add cap center vertices
        if (closeStartCap)
        {
            _vertices[currentVertIndex++] = positions[0];
        }
        
        if (closeEndCap)
        {
            _vertices[currentVertIndex++] = positions[positions.Length - 1];
        }

        _mesh.vertices = _vertices;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _meshFilter.mesh = _mesh;
    }

    private Vector2[] GenerateUVs(int positionCount)
    {
        // Calculate total vertices including caps
        int totalVertices = positionCount * sides;
        if (closeStartCap) totalVertices += 1;
        if (closeEndCap) totalVertices += 1;
        
        var uvs = new Vector2[totalVertices];

        // UVs for tube body
        for (int segment = 0; segment < positionCount; segment++)
        {
            for (int side = 0; side < sides; side++)
            {
                var vertIndex = (segment * sides + side);
                var u = side / (sides - 1f);
                var v = segment / (positionCount - 1f);

                uvs[vertIndex] = new Vector2(u, v);
            }
        }

        // UVs for caps (center of UV space)
        int capStartIndex = positionCount * sides;
        if (closeStartCap)
        {
            uvs[capStartIndex] = new Vector2(0.5f, 0);
        }
        
        if (closeEndCap)
        {
            int endCapIndex = capStartIndex;
            if (closeStartCap) endCapIndex++;
            uvs[endCapIndex] = new Vector2(0.5f, 1);
        }

        return uvs;
    }

    private int[] GenerateIndices(int positionCount)
    {
        // Calculate number of triangles
        int tubeTriangles = (positionCount - 1) * sides * 2;
        int capTriangles = 0;
        
        if (closeStartCap) capTriangles += sides;
        if (closeEndCap) capTriangles += sides;
        
        int totalTriangles = tubeTriangles + capTriangles;
        
        // Each triangle has 3 indices
        var indices = new int[totalTriangles * 3];

        // Generate indices for tube body
        var currentIndicesIndex = 0;
        for (int segment = 1; segment < positionCount; segment++)
        {
            for (int side = 0; side < sides; side++)
            {
                var vertIndex = (segment * sides + side);
                var prevVertIndex = vertIndex - sides;

                // Triangle one
                indices[currentIndicesIndex++] = prevVertIndex;
                indices[currentIndicesIndex++] = (side == sides - 1) ? (vertIndex - (sides - 1)) : (vertIndex + 1);
                indices[currentIndicesIndex++] = vertIndex;

                // Triangle two
                indices[currentIndicesIndex++] = (side == sides - 1) ? (prevVertIndex - (sides - 1)) : (prevVertIndex + 1);
                indices[currentIndicesIndex++] = (side == sides - 1) ? (vertIndex - (sides - 1)) : (vertIndex + 1);
                indices[currentIndicesIndex++] = prevVertIndex;
            }
        }

        // Generate indices for start cap - CORRECTED WINDING ORDER
        if (closeStartCap)
        {
            int centerVertexIndex = positionCount * sides;
            
            for (int side = 0; side < sides; side++)
            {
                // Reversed winding order for start cap so it faces outward
                indices[currentIndicesIndex++] = centerVertexIndex;
                indices[currentIndicesIndex++] = (side == sides - 1) ? 0 : (side + 1);
                indices[currentIndicesIndex++] = side;
            }
        }

        // Generate indices for end cap - CORRECTED WINDING ORDER
        if (closeEndCap)
        {
            int centerVertexIndex = positionCount * sides;
            if (closeStartCap) centerVertexIndex++;
            
            int lastRingStartIndex = (positionCount - 1) * sides;
            
            for (int side = 0; side < sides; side++)
            {
                // Correct winding order for end cap so it faces outward
                indices[currentIndicesIndex++] = centerVertexIndex;
                indices[currentIndicesIndex++] = lastRingStartIndex + side;
                indices[currentIndicesIndex++] = (side == sides - 1) ? lastRingStartIndex : (lastRingStartIndex + side + 1);
            }
        }

        return indices;
    }

    private Vector3[] CalculateCircle(int index)
    {
        var forward = Vector3.zero;
        
        // Calculate forward direction
        if (index == 0)
        {
            // First point - use direction to next point
            forward = (positions[index + 1] - positions[index]).normalized;
        }
        else if (index == positions.Length - 1)
        {
            // Last point - use direction from previous point
            forward = (positions[index] - positions[index - 1]).normalized;
        }
        else
        {
            // Middle points - use average of adjacent segments, but preserve length
            var dir1 = (positions[index] - positions[index - 1]).normalized;
            var dir2 = (positions[index + 1] - positions[index]).normalized;
            
            // Calculate angle between segments
            float angle = Vector3.Angle(dir1, dir2) * Mathf.Deg2Rad;
            
            // If angle is very sharp, use a different approach
            if (angle > Mathf.PI * 0.75f) // 135 degrees or more
            {
                // For very sharp corners, use the previous segment's direction
                forward = dir1;
            }
            else
            {
                // Normal case - average the directions and normalize
                // Use the formula that properly accounts for the angle between segments
                forward = (dir1 + dir2).normalized;
                
                // Adjust for miter length to maintain consistent tube thickness
                if (angle > 0.01f) // Only if there's a noticeable angle
                {
                    // Adjust by 1/sin(angle/2) to maintain tube thickness
                    float adjust = 1.0f / Mathf.Sin(angle * 0.5f);
                    // Clamp to avoid extreme values at very sharp corners
                    adjust = Mathf.Clamp(adjust, 1.0f, 2.0f);
                    forward = forward * adjust;
                }
            }
        }

        // Calculate perpendicular axes for the circle
        var up = Vector3.Cross(forward, Vector3.up);
        if (up.magnitude < 0.01f)
        {
            // If forward is parallel to up, use a different reference vector
            up = Vector3.Cross(forward, Vector3.right);
        }
        up.Normalize();
        
        var right = Vector3.Cross(up, forward).normalized;

        var circle = new Vector3[sides];
        var angleStep = (2 * Mathf.PI) / sides;
        var t = index / (positions.Length - 1f);
        
        // Calculate radius based on the selected mode
        float radius;
        switch (radiusMode)
        {
            case RadiusMode.StartEnd:
                radius = Mathf.Lerp(radiusOne, radiusTwo, t);
                break;
            case RadiusMode.Curve:
                radius = radiusCurve.Evaluate(t);
                break;
            case RadiusMode.Single:
            default:
                radius = radiusOne;
                break;
        }

        // Create the circle points
        for (int i = 0; i < sides; i++)
        {
            float angle = i * angleStep;
            float cosA = Mathf.Cos(angle);
            float sinA = Mathf.Sin(angle);
            
            circle[i] = positions[index] + right * (cosA * radius) + up * (sinA * radius);
        }

        return circle;
    }
}