using System;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;
using VInspector;

public enum AnimationSortMode
{
    None,           // Use order objects are found in scene
    ByName,         // Sort alphabetically by name
    ByDistance,     // Sort by distance from a reference point
    ByHierarchy,    // Sort by hierarchy order in scene
    Random,         // Randomize the order
    Custom,         // Use a custom ordered list
    DistanceFromPlayer  // Sort by distance from the player
}

public class TestAnimator : MonoBehaviour
{

    [Header("Settings")] 
    [SerializeField] private bool playOnTestLoading = true;
    [SerializeField] private bool playOnTestUnloading = true;
    [SerializeField] private bool resetOnDisable = true;
    
    [Header("Animation")]
    [SerializeField, Range(0f, 1f), Tooltip("Controls what portion of animation time is used for delays (0 = no delay, 1 = maximum delay)")] 
    private float delayTimeFactor = 0.5f;
    [SerializeField] private Ease scaleUpEase = Ease.OutBack;
    [SerializeField] private Ease scaleDownEase = Ease.InBack;
    [SerializeField] private AnimationSortMode sortMode = AnimationSortMode.None;
    [SerializeField, ShowIf("sortMode", AnimationSortMode.Custom)]
    private List<GameObject> customOrderedObjects = new List<GameObject>(); [EndIf]
    [SerializeField, ShowIf("sortMode", AnimationSortMode.ByDistance)] 
    private Transform distanceReferencePoint;[EndIf]
    [SerializeField, ShowIf("IsDistanceSort")] 
    private bool sortFromFarthest = false;[EndIf]
    
    
    [Header("Object Filtering")]
    [SerializeField] private bool findAllMeshesInScene = true;
    [SerializeField] private List<GameObject> additionalObjectsToAnimate = new List<GameObject>();
    [SerializeField] private List<GameObject> excludedObjects = new List<GameObject>();
    [SerializeField] private bool excludePlayer = true;
    [SerializeField] private bool excludeRobot = true;
    [SerializeField] private bool excludeFloor = true;
    
    [Header("Debug")] 
    [SerializeField, ReadOnly] private float totalAnimationTime; 
    [SerializeField, ReadOnly] private int numberOfObjectsToAnimate;
    [SerializeField, ReadOnly] private  List<GameObject> objectsToAnimate = new List<GameObject>();
    
    
    
    private readonly Dictionary<GameObject, Vector3> _originalScales = new Dictionary<GameObject, Vector3>();
    private Sequence _animationSequence;
    private bool _hasInitialized = false;
    private TestManager _testManager;

    public bool PlayOnTestLoading => playOnTestLoading;
    public bool PlayOnTestUnloading => playOnTestUnloading;
    public float TotalAnimationTime => totalAnimationTime;
    private bool IsDistanceSort => sortMode == AnimationSortMode.ByDistance || sortMode == AnimationSortMode.DistanceFromPlayer;

    private void Awake()
    {
        _testManager = GetComponent<TestManager>();
        
        Initialize();
    }
    

    private void OnDisable()
    {
        if (resetOnDisable)
        {
            ResetAllScales();
        }
    }
    

    private void Initialize()
    {
        if (_hasInitialized) return;
        
        objectsToAnimate.Clear();
        _originalScales.Clear();

        
        
        // Find all mesh renderers in the scene if option is enabled
        if (findAllMeshesInScene)
        {
            MeshRenderer[] allMeshes = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (MeshRenderer mesh in allMeshes)
            {
                if (!ShouldExclude(mesh.gameObject))
                {
                    objectsToAnimate.Add(mesh.gameObject);
                    _originalScales[mesh.gameObject] = mesh.transform.localScale;
                }
            }
        }
        
        // Add any additional objects
        foreach (GameObject obj in additionalObjectsToAnimate)
        {
            if (obj && !ShouldExclude(obj) && !objectsToAnimate.Contains(obj))
            {
                objectsToAnimate.Add(obj);
                _originalScales[obj] = obj.transform.localScale;
            }
        }
        
        // Sort the objects based on the selected sort mode
        SortObjectsBasedOnSortMode();
        
        // Update debug values
        numberOfObjectsToAnimate = objectsToAnimate.Count;
        _hasInitialized = true;
    }
    
    private bool ShouldExclude(GameObject obj)
    {
        // Direct match in excluded objects list
        if (excludedObjects.Contains(obj))
            return true;
            
        // Always check if object is a child of any excluded object (recursively)
        foreach (GameObject excludedObj in excludedObjects)
        {
            if (!excludedObj) continue;
            
            // Check if obj is a child of excludedObj (recursive check)
            if (IsChildOf(obj.transform, excludedObj.transform))
                return true;
        }
        
        // Check for Player exclusion
        if (excludePlayer && _testManager && _testManager.Player)
        {
            // Check if the object is the player or a child of the player
            GameObject playerObject = _testManager.Player.gameObject;
            if (obj == playerObject || IsChildOf(obj.transform, playerObject.transform))
                return true;
        }
        
        // Check for Robot exclusion
        if (excludeRobot && _testManager && _testManager.Robot)
        {
            // Check if the object is the robot or a child of the robot
            GameObject robotObject = _testManager.Robot.gameObject;
            if (obj == robotObject || IsChildOf(obj.transform, robotObject.transform))
                return true;
        }
        
        if (excludeFloor && _testManager && _testManager.FloorObject)
        {
            // Check if the object is the floor or a child of the floor
            GameObject floorObject = _testManager.FloorObject.gameObject;
            if (obj == floorObject || IsChildOf(obj.transform, floorObject.transform))
                return true;
        }
        
        return false;
    }

    // Helper method to check if a transform is a child of another transform (recursive)
    private bool IsChildOf(Transform child, Transform parent)
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
    
    private void SortObjectsBasedOnSortMode()
    {
        // First, remove any null or destroyed objects from the list
        objectsToAnimate.RemoveAll(obj => obj == null);
        
        switch (sortMode)
        {
            case AnimationSortMode.None:
                // Objects remain in the order they were found
                break;
                
            case AnimationSortMode.ByName:
                objectsToAnimate.Sort((a, b) => String.Compare(a.name, b.name, StringComparison.Ordinal));
                break;
                
            case AnimationSortMode.ByDistance:
                if (!distanceReferencePoint)
                {
                    distanceReferencePoint = transform;
                }
                
                // Sort by distance from reference point
                objectsToAnimate.Sort((a, b) => {
                    float distA = Vector3.Distance(a.transform.position, distanceReferencePoint.position);
                    float distB = Vector3.Distance(b.transform.position, distanceReferencePoint.position);
                    
                    return sortFromFarthest 
                        ? distB.CompareTo(distA) // Farthest first
                        : distA.CompareTo(distB); // Closest first
                });
                break;
                
            case AnimationSortMode.DistanceFromPlayer:
                if (_testManager && _testManager.Player)
                {
                    Transform playerTransform = _testManager.Player.transform;
                    
                    // Sort by distance from player
                    objectsToAnimate.Sort((a, b) => {
                        if (a == null || b == null) return 0;
                        
                        try {
                            float distA = Vector3.Distance(a.transform.position, playerTransform.position);
                            float distB = Vector3.Distance(b.transform.position, playerTransform.position);
                            
                            return sortFromFarthest 
                                ? distB.CompareTo(distA) // Farthest first
                                : distA.CompareTo(distB); // Closest first
                        }
                        catch (Exception) {
                            // If any exception occurs, consider the objects equal
                            return 0;
                        }
                    });
                }
                break;
                
            case AnimationSortMode.ByHierarchy:
                // Sort by sibling index in the hierarchy
                objectsToAnimate.Sort((a, b) => {
                    if (a == null || b == null) return 0;
                    
                    try {
                        // Get hierarchy paths and compare
                        string pathA = GetHierarchyPath(a.transform);
                        string pathB = GetHierarchyPath(b.transform);
                        return String.Compare(pathA, pathB, StringComparison.Ordinal);
                    }
                    catch (Exception) {
                        // If any exception occurs, consider the objects equal
                        return 0;
                    }
                });
                break;
                
            case AnimationSortMode.Random:
                // Fisher-Yates shuffle
                System.Random rng = new System.Random();
                int n = objectsToAnimate.Count;
                while (n > 1)
                {
                    n--;
                    int k = rng.Next(n + 1);
                    (objectsToAnimate[k], objectsToAnimate[n]) = (objectsToAnimate[n], objectsToAnimate[k]);
                }
                break;
                
            case AnimationSortMode.Custom:
                // First, add all objects from custom list that are in objects to animate
                List<GameObject> sortedList = new List<GameObject>();
                
                foreach (GameObject customObj in customOrderedObjects)
                {
                    if (customObj && objectsToAnimate.Contains(customObj))
                    {
                        sortedList.Add(customObj);
                        // Mark as processed by removing from original list
                        objectsToAnimate.Remove(customObj);
                    }
                }
                
                // Append any remaining objects that weren't in the custom list
                sortedList.AddRange(objectsToAnimate);
                
                // Replace the original list with our sorted one
                objectsToAnimate.Clear();
                objectsToAnimate.AddRange(sortedList);
                break;
        }
    }
    
    private string GetHierarchyPath(Transform transform)
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


    [Button]
    public void PlayScaleSequence(float animationTime)
    {
        if (!_hasInitialized)
        {
            Initialize();
        }
        else
        {
            // Clean up object list before sorting
            objectsToAnimate.RemoveAll(obj => obj == null);
            
            // Refresh original scales in case objects have changed
            foreach (GameObject obj in objectsToAnimate)
            {
                if (obj && !_originalScales.ContainsKey(obj))
                {
                    _originalScales[obj] = obj.transform.localScale;
                }
            }
            
            // Re-sort objects if already initialized, to ensure we're using the current sort mode
            SortObjectsBasedOnSortMode();
    
            // Update debug values
            numberOfObjectsToAnimate = objectsToAnimate.Count;
            totalAnimationTime = animationTime; // Total time is exactly animationTime
        }

        // Stop any running sequence
        _animationSequence.Stop();
        _animationSequence = Sequence.Create();

        // Set all objects to zero scale
        SetAllObjectsToZeroScale();
    
        // Calculate individual animation durations and delays
        float totalDelayTime = objectsToAnimate.Count > 1 
            ? animationTime * delayTimeFactor // portion of time used for delays
            : 0f;
    
        float individualDuration = objectsToAnimate.Count > 1 
            ? animationTime * (1f - delayTimeFactor) // remaining portion for actual animation
            : animationTime;

        // Create the animation sequence
        for (int i = 0; i < objectsToAnimate.Count; i++)
        {
            GameObject obj = objectsToAnimate[i];
            if (!obj || !_originalScales.ContainsKey(obj)) continue;
    
            // Calculate delay for each object (spread evenly across totalDelayTime)
            float delay = objectsToAnimate.Count > 1 
                ? i * (totalDelayTime / (objectsToAnimate.Count - 1)) 
                : 0f;
    
            _animationSequence.Group(
                Tween.Scale(
                    obj.transform,
                    startValue: Vector3.zero,
                    endValue: _originalScales[obj],
                    individualDuration,
                    ease: scaleUpEase,
                    startDelay: delay
                )
            );
        }
    }
    
    
    [Button]
    public void PlayReverseSequence(float animationTime)
    {
        if (!_hasInitialized)
        {
            Initialize();
        }
        else
        {
            // Clean up object list before sorting
            objectsToAnimate.RemoveAll(obj => obj == null);
            
            // Re-sort objects if already initialized
            SortObjectsBasedOnSortMode();
        }

        // Create a temporary reversed list for animation
        List<GameObject> reversedObjects = new List<GameObject>(objectsToAnimate);
        reversedObjects.Reverse();

        // Stop any running sequence
        _animationSequence.Stop();
        _animationSequence = Sequence.Create();

        // Ensure all objects are at their original scale before shrinking
        RestoreAllOriginalScales();
    
        // Calculate individual animation durations and delays
        float totalDelayTime = reversedObjects.Count > 1 
            ? animationTime * delayTimeFactor // portion of time used for delays
            : 0f;
    
        float individualDuration = reversedObjects.Count > 1 
            ? animationTime * (1f - delayTimeFactor) // remaining portion for actual animation
            : animationTime;

        // Create the animation sequence - from original scale to zero
        for (int i = 0; i < reversedObjects.Count; i++)
        {
            GameObject obj = reversedObjects[i];
            if (!obj || !_originalScales.ContainsKey(obj)) continue;
    
            // Calculate delay for each object (spread evenly across totalDelayTime)
            float delay = reversedObjects.Count > 1 
                ? i * (totalDelayTime / (reversedObjects.Count - 1)) 
                : 0f;
    
            _animationSequence.Group(
                Tween.Scale(
                    obj.transform,
                    startValue: _originalScales[obj],  // Start from original scale
                    endValue: Vector3.zero,            // Shrink to zero
                    individualDuration,
                    ease: scaleDownEase,
                    startDelay: delay
                )
            );
        }

        // Update debug values
        numberOfObjectsToAnimate = objectsToAnimate.Count;
        totalAnimationTime = animationTime; // Total time is exactly animationTime
    }


    public void SetAllObjectsToZeroScale()
    {
        foreach (var obj in objectsToAnimate)
        {
            if (obj)
            {
                obj.transform.localScale = Vector3.zero;
            }
        }
    }
    
    public void RestoreAllOriginalScales()
    {
        foreach (var obj in objectsToAnimate)
        {
            if (obj && _originalScales.TryGetValue(obj, out var scale))
            {
                obj.transform.localScale = scale;
            }
        }
    }

    [Button]
    private void ResetAllScales()
    {
        if (!_hasInitialized) return;
        
        // Stop any running sequence
        _animationSequence.Stop();
        
        // Reset all objects to their original scale
        RestoreAllOriginalScales();
    }

    // Method to refresh meshes from a newly loaded environment
    public void RefreshForNewEnvironment()
    {
        _hasInitialized = false;
        Initialize();
    }

    
    
    
    
    #region Editor Functions

#if UNITY_EDITOR
    [Button]
    private void RefreshObjectList()
    {
        // Reset initialization to force refreshing the object lists
        _hasInitialized = false;
        Initialize();
        Debug.Log($"Found {objectsToAnimate.Count} objects to animate");
    }
    
    [Button]
    private void FillCustomOrderFromCurrent()
    {
        if (!_hasInitialized)
        {
            Initialize();
        }
        
        // This allows you to use another sort method first, then save that order as custom
        customOrderedObjects.Clear();
        foreach (GameObject obj in objectsToAnimate)
        {
            if (obj)
            {
                customOrderedObjects.Add(obj);
            }
        }
        
        Debug.Log($"Added {customOrderedObjects.Count} objects to custom order list");
    }
    
    [Button]
    private void ReverseCurrentOrder()
    {
        if (!_hasInitialized)
        {
            Initialize();
        }
        
        objectsToAnimate.Reverse();
        Debug.Log("Reversed the animation order");
    }
#endif

    #endregion
}