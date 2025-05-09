using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VInspector;


[SelectionBase]
public class DynamicStairs : MonoBehaviour
{
    [Header("Dynamic Movement")]
    [SerializeField] private bool useDynamicMovement = true;
    [SerializeField] private bool flipMovement = false;
    [SerializeField] private bool useDithering;
    [SerializeField] private TargetType target = TargetType.Custom;
    [SerializeField, ShowIf("target", TargetType.Custom)] private Transform targetTransform;[EndIf]
    [SerializeField] private float minDistanceThreshold = 2f;
    [SerializeField] private float maxDistanceThreshold = 5f;
    [SerializeField] private float stepMovementSpeed = 10f;
    [SerializeField] private float checkDistanceThreshold = 15f;
    [SerializeField] private Vector3 checkPositionOffset;
    [SerializeField] private bool resetPositionsOnStart = true;
    
    
    [Foldout("Step Building")]
    [Header("Steps")]
    [SerializeField] private int numberOfSteps = 10;
    [SerializeField] private float horizontalOverlap;
    [SerializeField] private float verticalOverlap;
    [SerializeField] private bool useStepDepthForSpacing = true; 
    [SerializeField] private bool separateColliderFromVisual;
    [SerializeField] private Vector3 stepSize = new Vector3(0.2f, 0.2f, 3f);
    [SerializeField] private Vector3 stepPositonOffset;
    
    [Header("Base")]
    [SerializeField] private bool useBase = true;
    [SerializeField] private float baseHeightOffset = 0.05f; 
    [SerializeField] private float baseHeightMultiplier = 1.0f;
    [SerializeField] private float baseWidthMultiplier = 1.2f;
    [SerializeField] private float baseLengthMultiplier = 1.0f;
    
    [Header("Connector")]
    [SerializeField] private bool useConnector = true;
    [SerializeField] private Vector3 connectorOffset = new Vector3(0f, 0f, 0f);
    
    [Header("References")]
    [SerializeField] private GameObject stepPrefab;
    [SerializeField] private GameObject basePrefab;
    [SerializeField] private TubeRenderer connectorPrefab;
    [EndFoldout]
    
    private enum TargetType { Player, Robot, Custom }
    private readonly Dictionary<GameObject, Vector3> _stepsPositions = new Dictionary<GameObject, Vector3>();
    private Vector3 _stepsBottomPosition;
    private readonly List<GameObject> _colliderObjects = new List<GameObject>();
    private readonly List<GameObject> _visualObjects = new List<GameObject>();
    private GameObject _baseObject;
    private TubeRenderer _connector;
    private readonly Dictionary<GameObject, Material> _stepMaterials = new Dictionary<GameObject, Material>();
    private static readonly int DitherFlip = Shader.PropertyToID("_Dither_Flip");
    private static readonly int WorldPosition = Shader.PropertyToID("_Dither_World_Position");
    private bool _materialsInitialized;

    private void Awake()
    {
        if (separateColliderFromVisual)
        {
            CategorizeStepObjects();
        }
        
        StoreStepPositions();
        
        if (useConnector)
        {
            _connector = GetComponentInChildren<TubeRenderer>();
        }
        
        if (useDithering)
        {
            // Create material instances for each step
            CreateMaterialInstances();
            _materialsInitialized = true;
        }
        
        if (resetPositionsOnStart) ResetStepsToBottomPosition();
    }
    
    private void Update()
    {
        // Only proceed with updates if dynamic movement is enabled
        if (!useDynamicMovement) return;
        
        Transform currentTarget = CurrentTarget();
        if (!currentTarget) return;
        
        // Calculate distance between this object and the target (ignoring Y)
        Vector3 objectPosXZ = new Vector3(transform.position.x + checkPositionOffset.x, 0 + checkPositionOffset.y, transform.position.z + checkPositionOffset.z);
        Vector3 targetPosXZ = new Vector3(currentTarget.position.x, 0, currentTarget.position.z);
        float distanceToTarget = Vector3.Distance(objectPosXZ, targetPosXZ);
        
        // Only proceed with step movements if target is within check distance threshold
        if (distanceToTarget <= checkDistanceThreshold)
        {
            UpdateStepPositions(targetPosXZ);
            UpdateStepMaterials(targetPosXZ);
            UpdateConnectorPositions();
        }
        else
        {
            // If we're far away, we can optionally reset steps to their original positions
            // This is commented out as it depends on the desired behavior
            // ResetStepsToOriginalPositions();
        }
    }
    
    private void UpdateConnectorPositions()
    {
        // Skip if connector is not initialized
        if (!_connector || !useConnector) return;
    
        List<Vector3> connectorPoints = new List<Vector3>();
    
        // Get all step objects in the correct order
        List<GameObject> stepsToConnect = separateColliderFromVisual ? _visualObjects : new List<GameObject>();
    
        if (!separateColliderFromVisual)
        {
            // If not using separate colliders, get all child objects
            foreach (Transform child in transform)
            {
                // Check if the object is a step (and not the base or connector)
                if (child.name.StartsWith("Step_") && child.gameObject != _baseObject)
                {
                    stepsToConnect.Add(child.gameObject);
                }
            }
        }
    
        // Sort the steps by their index in the name (instead of Y position)
        stepsToConnect.Sort((a, b) => {
            // Extract index from the name (e.g., "Step_5" -> 5)
            int indexA = ExtractIndexFromName(a.name);
            int indexB = ExtractIndexFromName(b.name);
            return indexA.CompareTo(indexB);
        });
    
        // Collect positions for each step (in local space of the connector)
        foreach (var step in stepsToConnect)
        {
            // Get the step's position in world space
            Vector3 stepWorldPos = step.transform.position;
        
            // Convert to connector's local space and add offset
            Vector3 connectorPoint = _connector.transform.InverseTransformPoint(stepWorldPos) + connectorOffset;
        
            // Add to the list of points
            connectorPoints.Add(connectorPoint);
        }
    
        // Update the tube renderer with the new positions
        if (connectorPoints.Count > 1)
        {
            _connector.Positions = connectorPoints.ToArray();
        }
    }
    
    private void UpdateStepMaterials(Vector3 targetPosXZ)
    {
        // Skip if materials aren't initialized or dithering is disabled
        if (!_materialsInitialized || !useDithering) return;
        
        foreach (var pair in _stepMaterials)
        {
            GameObject stepObject = pair.Key;
            Material material = pair.Value;
            
            if (material && material.HasProperty(WorldPosition) && _stepsPositions.TryGetValue(stepObject, out Vector3 originalLocalPosition))
            {
                // Transform the original local position to world space
                Vector3 originalWorldPos = transform.TransformPoint(originalLocalPosition);
                
                // Calculate horizontal distance using the original position (ignoring Y)
                Vector3 origPosXZ = new Vector3(originalWorldPos.x, 0, originalWorldPos.z);
                float distance = Vector3.Distance(targetPosXZ, origPosXZ);
                
                // Get the step's current world position (for shader property)
                Vector3 stepWorldPos = stepObject.transform.position;
                
                // Set the bottom position value based on distance
                Vector3 worldBottomPosition;
                
                if (distance <= minDistanceThreshold)
                {
                    // At original position, no dithering
                    worldBottomPosition = stepWorldPos;
                }
                else if (distance >= maxDistanceThreshold)
                {
                    // At maximum distance, fully dithered to bottom
                    worldBottomPosition = transform.TransformPoint(_stepsBottomPosition);
                }
                else
                {
                    // Interpolate
                    float t = (distance - minDistanceThreshold) / (maxDistanceThreshold - minDistanceThreshold);
                    Vector3 localBottomPosition = Vector3.Lerp(stepObject.transform.localPosition, _stepsBottomPosition, t);
                    worldBottomPosition = transform.TransformPoint(localBottomPosition);
                }
                
                // Update the shader property
                material.SetVector(WorldPosition, worldBottomPosition);
            }
        }
    }
    
    private void CategorizeStepObjects()
    {
        _colliderObjects.Clear();
        _visualObjects.Clear();
        
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("StepCollider_"))
            {
                _colliderObjects.Add(child.gameObject);
            }
            else if (child.name.StartsWith("StepVisual_") || child.name.StartsWith("Step_"))
            {
                _visualObjects.Add(child.gameObject);
            }
        }
    }
    
    private void UpdateStepPositions(Vector3 targetPosXZ)
    {
        if (separateColliderFromVisual)
        {
            // When using separate colliders, only update visual objects
            foreach (GameObject visualStep in _visualObjects)
            {
                if (!_stepsPositions.TryGetValue(visualStep, out Vector3 originalPosition)) continue;
                
                // Calculate horizontal distance for this specific step (ignoring Y)
                Vector3 stepWorldPos = visualStep.transform.position;
                Vector3 stepPosXZ = new Vector3(stepWorldPos.x, 0, stepWorldPos.z);
                float distance = Vector3.Distance(targetPosXZ, stepPosXZ);
                
                UpdateStepHeight(visualStep, originalPosition, distance);
            }
        }
        else
        {
            // When not separating, update all step objects (but not the base or connector)
            foreach (Transform stepTransform in transform)
            {
                // Skip if this is not a step (e.g., it's the base)
                if (!stepTransform.name.StartsWith("Step_")) continue;
                
                if (!_stepsPositions.TryGetValue(stepTransform.gameObject, out Vector3 originalPosition)) continue;
                
                // Calculate horizontal distance for this specific step (ignoring Y)
                Vector3 stepWorldPos = stepTransform.position;
                Vector3 stepPosXZ = new Vector3(stepWorldPos.x, 0, stepWorldPos.z);
                float distance = Vector3.Distance(targetPosXZ, stepPosXZ);
                
                UpdateStepHeight(stepTransform.gameObject, originalPosition, distance);
            }
        }
    }
    
    private void UpdateStepHeight(GameObject step, Vector3 originalPosition, float distance)
    {
        // Calculate target Y position based on distance
        float targetYPosition;
        
        if (flipMovement)
        {
            // FLIPPED LOGIC: When flip is true, closer = bottom, farther = original
            if (distance >= maxDistanceThreshold)
            {
                // At the original height when far enough
                targetYPosition = originalPosition.y;
            }
            else if (distance <= minDistanceThreshold)
            {
                // At bottom position when close enough
                targetYPosition = _stepsBottomPosition.y;
            }
            else
            {
                // Interpolate between bottom and original height based on distance
                float t = (distance - minDistanceThreshold) / (maxDistanceThreshold - minDistanceThreshold);
                targetYPosition = Mathf.Lerp(_stepsBottomPosition.y, originalPosition.y, t);
            }
        }
        else
        {
            // ORIGINAL LOGIC: When flip is false, closer = original, farther = bottom
            if (distance <= minDistanceThreshold)
            {
                // At the original height when close enough
                targetYPosition = originalPosition.y;
            }
            else if (distance >= maxDistanceThreshold)
            {
                // At bottom position when far enough
                targetYPosition = _stepsBottomPosition.y;
            }
            else
            {
                // Interpolate between original and bottom height based on distance
                float t = (distance - minDistanceThreshold) / (maxDistanceThreshold - minDistanceThreshold);
                targetYPosition = Mathf.Lerp(originalPosition.y, _stepsBottomPosition.y, t);
            }
        }
        
        // Get the current local position
        Vector3 currentLocalPos = step.transform.localPosition;
        
        // Smoothly interpolate to the target height
        float newY = Mathf.Lerp(currentLocalPos.y, targetYPosition, Time.deltaTime * stepMovementSpeed);
        
        // Apply the new height while keeping X and Z unchanged
        step.transform.localPosition = new Vector3(currentLocalPos.x, newY, currentLocalPos.z);
    }
        

    private void StoreStepPositions()
    {
        _stepsPositions.Clear();
        
        if (separateColliderFromVisual)
        {
            // When using separate colliders, only store positions for visual objects
            foreach (GameObject visualStep in _visualObjects)
            {
                _stepsPositions[visualStep] = visualStep.transform.localPosition;
            }
        }
        else
        {
            // When not separating, store positions for all step child objects (not the base)
            foreach (Transform child in transform)
            {
                // Only store positions for actual step objects
                if (child.name.StartsWith("Step_"))
                {
                    _stepsPositions[child.gameObject] = child.localPosition;
                }
            }
        }
        
        // Store the bottom-most position for the steps
        if (_stepsPositions.Count > 0)
        {
            _stepsBottomPosition = _stepsPositions.First().Value;
        }
    }
    
    private void CreateMaterialInstances()
    {
        _stepMaterials.Clear();
        
        // Determine which objects to process
        List<GameObject> objectsToProcess = separateColliderFromVisual ? _visualObjects : new List<GameObject>();
        
        if (!separateColliderFromVisual)
        {
            // If not using separate colliders, process all step objects (but not the base)
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Step_"))
                {
                    objectsToProcess.Add(child.gameObject);
                }
            }
        }
        
        // Create material instances for each object
        foreach (GameObject stepObject in objectsToProcess)
        {
            // Get renderers (could be on the object or its children)
            Renderer[] renderers = stepObject.GetComponentsInChildren<Renderer>();
            
            foreach (Renderer rend in renderers)
            {
                // Create instances of all materials
                Material[] sharedMaterials = rend.sharedMaterials;
                Material[] instanceMaterials = new Material[sharedMaterials.Length];
                
                for (int i = 0; i < sharedMaterials.Length; i++)
                {
                    // Create a new instance of this material
                    instanceMaterials[i] = new Material(sharedMaterials[i]);
                    
                    // Store this material instance if it has the shader property
                    if (instanceMaterials[i].HasProperty(WorldPosition))
                    {
                        _stepMaterials[stepObject] = instanceMaterials[i];
                    }
                }
                
                // Assign the instance materials back to the renderer
                rend.materials = instanceMaterials;
                foreach (var material in instanceMaterials)
                {
                    if (material.HasProperty(DitherFlip))
                    {
                        material.SetFloat(DitherFlip, 1);
                    }
                }
            }
        }
    }
    
    
    [Button]
    private void ResetStepsToOriginalPositions()
    {
        if (!Application.isPlaying) return;
        
        List<GameObject> stepsToReset = separateColliderFromVisual ? _visualObjects : new List<GameObject>();
        if (!separateColliderFromVisual)
        {
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Step_"))
                {
                    stepsToReset.Add(child.gameObject);
                }
            }
        }
        
        foreach (GameObject step in stepsToReset)
        {
            // Get the original position
            if (_stepsPositions.TryGetValue(step, out Vector3 originalPosition))
            {
                // Set the step's position to the original one
                step.transform.localPosition = originalPosition;
            }
        }

        UpdateConnectorPositions();
    }
    
    [Button]
    private void ResetStepsToBottomPosition()
    {
        if (!Application.isPlaying) return;
        
        List<GameObject> stepsToReset = separateColliderFromVisual ? _visualObjects : new List<GameObject>();
        
        if (!separateColliderFromVisual)
        {
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Step_"))
                {
                    stepsToReset.Add(child.gameObject);
                }
            }
        }
        
        foreach (GameObject step in stepsToReset)
        {
            // Get the current local position
            Vector3 currentLocalPos = step.transform.localPosition;
            
            // Set the Y position to the bottom position
            step.transform.localPosition = new Vector3(currentLocalPos.x, _stepsBottomPosition.y, currentLocalPos.z);
        }

        UpdateConnectorPositions();
    }
    
    
        


    #region helper methods ---------------------------------------------------------------------

    private int ExtractIndexFromName(string name)
    {
        // Expected formats: "Step_X", "StepVisual_X", "StepCollider_X"
        string[] parts = name.Split('_');
        if (parts.Length > 1 && int.TryParse(parts[^1], out int index))
        {
            return index;
        }
    
        // If we can't extract a valid index, return a large number to put it at the end
        return int.MaxValue;
    }
    
    private Transform CurrentTarget()
    {
        switch (target)
        {
            case TargetType.Player:
                if (TestManager.Instance && TestManager.Instance.Player) return TestManager.Instance.Player.transform;
                return null;
            case TargetType.Robot:
                if (TestManager.Instance && TestManager.Instance.Robot) return TestManager.Instance.Robot.transform;
                return null;
            case TargetType.Custom:
                if (targetTransform) return targetTransform;
                return null;
            default:
                return null;
        }
    }

    #endregion helper methods ---------------------------------------------------------------------

    

    #region public methods ---------------------------------------------------------------------

    public void SetTargetModePlayer()
    {
        target = TargetType.Player;
    }
    
    public void SetTargetModeRobot()
    {
        target = TargetType.Robot;
    }
    
    public void SetTargetModeCustom()
    {
        target = TargetType.Custom;
    }
    public void SetCustomTarget(Transform tar)
    {
        targetTransform = tar;
    }

    #endregion public methods ---------------------------------------------------------------------

    
    
    #region steps building ---------------------------------------------------------------------

    [Button]
    private void RemoveAllSteps()
    {
        // Clear existing steps
        var currentSteps = new List<GameObject>();
        foreach (Transform child in transform)
        {
            currentSteps.Add(child.gameObject);
        }
        foreach (var step in currentSteps)
        {
            DestroyImmediate(step);
        }
        
        // Clear stored positions and lists
        _stepsPositions.Clear();
        _colliderObjects.Clear();
        _visualObjects.Clear();
        _baseObject = null;
        
        // Remove connector if it exists
        if (_connector != null)
        {
            DestroyImmediate(_connector.gameObject);
            _connector = null;
        }
    }
    
    [Button]
    public void RebuildSteps()
    {
        if (stepPrefab == null)
        {
            Debug.LogError("Step prefab is not assigned!");
            return;
        }
        
        RemoveAllSteps();

        // Calculate the actual base height considering multiplier
        float actualBaseHeight = stepSize.y * baseHeightMultiplier;
        
        // Set the starting position for steps to be at the top of the base
        Vector3 currentPosition = new Vector3(0, actualBaseHeight, 0);
        _stepsBottomPosition = new Vector3(0, 0, 0); // Store the bottom-most position at 0
        
        // Calculate step depth (after rotation, x becomes depth)
        float stepDepth = stepSize.x;
        
        // Calculate horizontal increment based on step depth and overlap
        float horizontalIncrement = useStepDepthForSpacing ? 
            stepDepth * (1 - horizontalOverlap) : 
            stepSize.z * 0.5f; // Default spacing if not using depth

        // Create base first so it appears behind the steps in hierarchy
        if (useBase && basePrefab != null)
        {
            CreateStairsBase(horizontalIncrement);
        }
        
        // Create connector as the second child in hierarchy (right after the base)
        if (useConnector && connectorPrefab != null)
        {
            CreateConnector();
        }
        
        // Now create the steps
        for (int i = 0; i < numberOfSteps; i++)
        {
            // Track objects we create
            GameObject visualObject;
            
            // If we're using separate colliders
            if (separateColliderFromVisual)
            {
                // Create a static collider object that will not move
                GameObject colliderObject = new GameObject("StepCollider_" + i)
                {
                    transform =
                    {
                        parent = transform,
                        localPosition = currentPosition + stepPositonOffset,
                        localRotation = Quaternion.Euler(0, 90, 0),
                        localScale = stepSize
                    }
                };

                // Add a box collider component to the collider object
                colliderObject.AddComponent<BoxCollider>();
                
                // Add to collider objects list
                _colliderObjects.Add(colliderObject);
                
                // Create a visual step without collider
                visualObject = Instantiate(stepPrefab, transform);
                visualObject.name = "StepVisual_" + i;
                
                // Remove any colliders from the visual step
                Collider[] visualColliders = visualObject.GetComponents<Collider>();
                foreach (var visualCollider in visualColliders)
                {
                    DestroyImmediate(visualCollider);
                }
                
                // Also remove any colliders from child objects if they exist
                Collider[] childColliders = visualObject.GetComponentsInChildren<Collider>(true);
                foreach (var childCollider in childColliders)
                {
                    DestroyImmediate(childCollider);
                }
            }
            else
            {
                // Create a regular step with collider
                visualObject = Instantiate(stepPrefab, transform);
                visualObject.name = "Step_" + i;
            }
            
            // Add to visual objects list
            _visualObjects.Add(visualObject);
            
            // Position the step (visual or regular) with the added step position offset
            visualObject.transform.localPosition = currentPosition + stepPositonOffset;
            
            // Rotate the step 90 degrees around the Y-axis
            visualObject.transform.localRotation = Quaternion.Euler(0, 90, 0);
            
            // Scale the step to match the desired size
            visualObject.transform.localScale = stepSize;
            
            // Store the original position for this step (including offset)
            _stepsPositions[visualObject] = currentPosition + stepPositonOffset;
            
            // Move position for the next step (up and forward)
            currentPosition.y += stepSize.y * (1 - verticalOverlap); // Vertical rise with overlap
            currentPosition.z += horizontalIncrement; // Horizontal run
        }
        
        // Update connector positions if we need to
        if (_connector != null)
        {
            UpdateConnectorPositions();
        }
        
        // If we're in Play mode, we need to call CreateMaterialInstances()
        // because Awake won't be called again when rebuilding
        if (Application.isPlaying && !_materialsInitialized)
        {
            CreateMaterialInstances();
            _materialsInitialized = true;
        }
    }
    
    private void CreateConnector()
    {
        // Remove existing connector if there is one
        if (_connector != null)
        {
            DestroyImmediate(_connector.gameObject);
        }
        
        // Create a new connector from the prefab
        GameObject connectorObject = Instantiate(connectorPrefab.gameObject, transform);
        connectorObject.name = "StairsConnector";
        
        // Get the TubeRenderer component
        _connector = connectorObject.GetComponent<TubeRenderer>();
        if (_connector == null)
        {
            Debug.LogError("Connector prefab does not have a TubeRenderer component!");
            DestroyImmediate(connectorObject);
            return;
        }
        
        // Set up empty connector positions initially (will be updated later)
        _connector.Positions = Array.Empty<Vector3>();
    }
    
    private void CreateStairsBase(float horizontalIncrement)
    {
        if (basePrefab == null)
        {
            Debug.LogWarning("Base prefab is not assigned. Skipping base creation.");
            return;
        }
        
        // Calculate the total length of the staircase
        float totalStairsLength = horizontalIncrement * (numberOfSteps - 1);
        
        // Create the base object
        _baseObject = Instantiate(basePrefab, transform);
        _baseObject.name = "StairsBase";
        
        // Calculate base position
        // - X position remains unchanged
        // - Y position at the bottom (zero) with offset
        // - Z position is half the total depth to center it under the stairs
        Vector3 basePosition = new Vector3(
            0f,                       // X position (unchanged)
            -baseHeightOffset,        // Y position at bottom with offset
            totalStairsLength / 2     // Z position (centered under all steps)
        );
        
        // Set the base position
        _baseObject.transform.localPosition = basePosition;
        
        // Rotate the base 
        _baseObject.transform.localRotation = Quaternion.Euler(0, 0, -90);
        
        Vector3 baseScale = new Vector3(
            stepSize.y * baseHeightMultiplier,          // Height = step height * height multiplier
            stepSize.z * baseWidthMultiplier,           // Width = step width * width multiplier
            (totalStairsLength + stepSize.x) * baseLengthMultiplier  // Length = total stair length * length multiplier
        );
        
        // Set the base scale
        _baseObject.transform.localScale = baseScale;
    }

    #endregion steps building ---------------------------------------------------------------------
    
    
    
    #region Editor ------------------------------------------------------------------------------



    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        
        // Calculate the actual base height considering multiplier
        float actualBaseHeight = stepSize.y * baseHeightMultiplier;
        
        // Start from a position that accounts for the base height
        Vector3 currentLocalPosition = new Vector3(0, actualBaseHeight, 0);
        
        // Calculate step depth (after rotation, x becomes depth)
        float stepDepth = stepSize.x;
        
        // Calculate horizontal increment based on step depth and overlap
        float horizontalIncrement = useStepDepthForSpacing ? 
            stepDepth * (1 - horizontalOverlap) : 
            stepSize.z * 0.5f; // Default spacing if not using depth
            
        // Draw base gizmo
        if (useBase && basePrefab)
        {
            Gizmos.color = Color.blue;
    
            // Calculate the total length of the staircase
            float totalStairsLength = horizontalIncrement * (numberOfSteps - 1);
    
            // Calculate base position (same as in CreateStairsBase)
            Vector3 basePosition = new Vector3(
                0f,
                -baseHeightOffset,
                totalStairsLength / 2
            );
    
            // Convert to world space
            Vector3 baseWorldPosition = transform.TransformPoint(basePosition);
    
            // Calculate base scale (same as in CreateStairsBase)
            Vector3 baseScale = new Vector3(
                stepSize.y * baseHeightMultiplier,
                stepSize.z * baseWidthMultiplier,
                (totalStairsLength + stepSize.x) * baseLengthMultiplier
            );
    
            // Get the object's rotation to apply to the gizmo
            Quaternion baseWorldRotation = transform.rotation * Quaternion.Euler(0, 0, -90);
    
            // Set up matrix for the gizmo
            Matrix4x4 baseGizmoMatrix = Matrix4x4.TRS(
                baseWorldPosition,
                baseWorldRotation,
                baseScale
            );
    
            Gizmos.matrix = baseGizmoMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = Matrix4x4.identity;
        }
        
        // Draw steps gizmos (with step position offset applied)
        Gizmos.color = Color.yellow;
        for (int i = 0; i < numberOfSteps; i++)
        {
            // Apply the step position offset to the gizmo position
            Vector3 stepPosition = currentLocalPosition + stepPositonOffset;
            
            // Convert local position to world position for the gizmo
            Vector3 worldPosition = transform.TransformPoint(stepPosition);
            
            // Get the object's rotation to apply to the gizmo
            Quaternion worldRotation = transform.rotation * Quaternion.Euler(0, 90, 0);
            
            // Set up matrix for the gizmo
            Matrix4x4 gizmoMatrix = Matrix4x4.TRS(
                worldPosition,
                worldRotation,
                stepSize
            );
            
            Gizmos.matrix = gizmoMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = Matrix4x4.identity;
            
            // Move position for the next step (in local space)
            currentLocalPosition.y += stepSize.y * (1 - verticalOverlap); // Vertical rise with overlap
            currentLocalPosition.z += horizontalIncrement; // Horizontal run
        }
        
        // Draw check distance threshold in blue
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + checkPositionOffset, checkDistanceThreshold);
            
        // Draw min distance threshold in green
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, minDistanceThreshold);
            
        // Draw max distance threshold in red
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDistanceThreshold);

    }

    #endregion Editor ------------------------------------------------------------------------------
}