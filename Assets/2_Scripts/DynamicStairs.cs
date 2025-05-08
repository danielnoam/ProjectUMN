using System;
using System.Collections.Generic;
using UnityEngine;
using VInspector;

public class DynamicStairs : MonoBehaviour
{
    [Header("Dynamic Movement")]
    [SerializeField] private bool useDynamicMovement = true;
    [SerializeField] private TargetType target = TargetType.Custom;
    [SerializeField, ShowIf("target", TargetType.Custom)] private Transform targetTransform;[EndIf]
    [SerializeField] private float minDistanceThreshold = 2f;
    [SerializeField] private float maxDistanceThreshold = 5f;
    [SerializeField] private float stepMovementSpeed = 10f;
    
    [Header("Step Building")]
    [SerializeField] private int numberOfSteps = 10;
    [SerializeField] private float horizontalOverlap;
    [SerializeField] private bool useStepDepthForSpacing = true; 
    [SerializeField] private bool separateColliderFromVisual;
    [SerializeField] private Vector3 stepSize = new Vector3(0.2f, 0.2f, 3f);
    [SerializeField] private GameObject stepPrefab;
    
    private enum TargetType { Player, Robot, Custom }
    private readonly Dictionary<GameObject, Vector3> _stepsPositions = new Dictionary<GameObject, Vector3>();
    private Vector3 _stepsBottomPosition;
    private readonly List<GameObject> _colliderObjects = new List<GameObject>();
    private readonly List<GameObject> _visualObjects = new List<GameObject>();
    private Material _stepMaterial;
    private static readonly int WorldPosition = Shader.PropertyToID("_Dither_World_Position");

    private void Awake()
    {
        if (separateColliderFromVisual)
        {
            CategorizeStepObjects();
        }
        StoreStepPositions();
        _stepsBottomPosition = Vector3.zero;
    }
    
    private void Update()
    {
        UpdateStepPositions();
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
    
    private void UpdateStepPositions()
    {
        if (!useDynamicMovement) return;
        
        Transform currentTarget = CurrentTarget();
        if (!currentTarget) return;
        
        Vector3 targetPosXZ = new Vector3(currentTarget.position.x, 0, currentTarget.position.z);
        
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
            // When not separating, update all step objects (original behavior)
            foreach (Transform stepTransform in transform)
            {
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
            // When not separating, store positions for all child objects (original behavior)
            foreach (Transform child in transform)
            {
                _stepsPositions[child.gameObject] = child.localPosition;
            }
        }
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

    #region Editor -----------------------------------

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

        Vector3 currentPosition = Vector3.zero;
        _stepsBottomPosition = currentPosition; // Store the bottom-most position
        
        // Calculate step depth (after rotation, x becomes depth)
        float stepDepth = stepSize.x;
        
        // Calculate horizontal increment based on step depth and overlap
        float horizontalIncrement = useStepDepthForSpacing ? 
            stepDepth * (1 - horizontalOverlap) : 
            stepSize.z * 0.5f; // Default spacing if not using depth

        for (int i = 0; i < numberOfSteps; i++)
        {
            // Track objects we create
            GameObject visualObject;
            
            // If we're using separate colliders
            if (separateColliderFromVisual)
            {
                // Create a static collider object that will not move
                GameObject colliderObject = new GameObject("StepCollider_" + i);
                colliderObject.transform.parent = transform;
                colliderObject.transform.localPosition = currentPosition;
                colliderObject.transform.localRotation = Quaternion.Euler(0, 90, 0);
                colliderObject.transform.localScale = stepSize;
                
                // Add a box collider component to the collider object
                BoxCollider boxCollider = colliderObject.AddComponent<BoxCollider>();
                
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
            
            // Position the step (visual or regular)
            visualObject.transform.localPosition = currentPosition;
            
            // Rotate the step 90 degrees around the Y-axis
            visualObject.transform.localRotation = Quaternion.Euler(0, 90, 0);
            
            // Scale the step to match the desired size
            visualObject.transform.localScale = stepSize;
            
            // Store the original position for this step
            _stepsPositions[visualObject] = currentPosition;
            
            // Move position for the next step (up and forward)
            currentPosition.y += stepSize.y; // Vertical rise
            currentPosition.z += horizontalIncrement; // Horizontal run
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        
        // Start from a local origin (Vector3.zero in local space)
        Vector3 currentLocalPosition = Vector3.zero;
        
        // Calculate step depth (after rotation, x becomes depth)
        float stepDepth = stepSize.x;
        
        // Calculate horizontal increment based on step depth and overlap
        float horizontalIncrement = useStepDepthForSpacing ? 
            stepDepth * (1 - horizontalOverlap) : 
            stepSize.z * 0.5f; // Default spacing if not using depth
        
        for (int i = 0; i < numberOfSteps; i++)
        {
            // Convert local position to world position for the gizmo
            Vector3 worldPosition = transform.TransformPoint(currentLocalPosition);
            
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
            currentLocalPosition.y += stepSize.y;
            currentLocalPosition.z += horizontalIncrement;
        }
        
        // Draw distance thresholds if a target is available
        Transform currentTarget = CurrentTarget();
        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, minDistanceThreshold);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, maxDistanceThreshold);
        }
    }

    #endregion Editor -----------------------------------
}