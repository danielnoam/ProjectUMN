using System;
using System.Collections;
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

public class MeshScaleSequence : MonoBehaviour
{
    public static MeshScaleSequence Instance { get; private set; }
    
    
    [Header("Settings")]
    [SerializeField] private bool playOnAwake = false;
    [SerializeField] private bool resetOnDisable = true;
    
    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float delayBetweenObjects = 0.1f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;
    [SerializeField] private AnimationSortMode sortMode = AnimationSortMode.None;
    [SerializeField, ShowIf("sortMode", AnimationSortMode.Custom)]
    private List<GameObject> customOrderedObjects = new List<GameObject>();
    [EndIf]
    [SerializeField, ShowIf("sortMode", AnimationSortMode.ByDistance)] 
    private Transform distanceReferencePoint;
    [SerializeField, ShowIf("IsDistanceSort")] 
    private bool sortFromFarthest = false;
    [EndIf]
    
    [Header("Object Filtering")]
    [SerializeField] private bool findAllMeshesInScene = true;
    [SerializeField] private List<GameObject> additionalObjectsToAnimate = new List<GameObject>();
    [SerializeField] private List<GameObject> excludedObjects = new List<GameObject>();
    [SerializeField] private bool excludeChildren = true;
    [SerializeField] private bool excludePlayer = true;
    [SerializeField] private bool excludeRobot = true;
    
    [Header("Debug")] 
    [SerializeField, CustomAttribute.ReadOnly] private float totalAnimationTime; 
    [SerializeField, CustomAttribute.ReadOnly] private int numberOfObjectsToAnimate;
    
    private readonly Dictionary<GameObject, Vector3> _originalScales = new Dictionary<GameObject, Vector3>();
    private readonly List<GameObject> _objectsToAnimate = new List<GameObject>();
    private Sequence _animationSequence;
    private bool _hasInitialized = false;
    
    // Helper property for VInspector
    private bool IsDistanceSort => sortMode == AnimationSortMode.ByDistance || sortMode == AnimationSortMode.DistanceFromPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        
        
        if (playOnAwake)
        {
            // We delay initialization to ensure all objects are properly set up
            StartCoroutine(InitializeAndPlayDelayed());
        }
        else
        {
            Initialize();
        }
    }

    private IEnumerator InitializeAndPlayDelayed()
    {
        // Small delay to ensure scene is fully loaded
        yield return new WaitForEndOfFrame();
        
        Initialize();
        PlayScaleSequence();
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
        
        _objectsToAnimate.Clear();
        _originalScales.Clear();
        
        // Find all mesh renderers in the scene if option is enabled
        if (findAllMeshesInScene)
        {
            MeshRenderer[] allMeshes = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (MeshRenderer mesh in allMeshes)
            {
                if (!ShouldExclude(mesh.gameObject))
                {
                    _objectsToAnimate.Add(mesh.gameObject);
                    _originalScales[mesh.gameObject] = mesh.transform.localScale;
                }
            }
        }
        
        // Add any additional objects
        foreach (GameObject obj in additionalObjectsToAnimate)
        {
            if (obj && !ShouldExclude(obj) && !_objectsToAnimate.Contains(obj))
            {
                _objectsToAnimate.Add(obj);
                _originalScales[obj] = obj.transform.localScale;
            }
        }
        
        // Sort the objects based on the selected sort mode
        SortObjectsBasedOnSortMode();
        
        // Update debug values
        numberOfObjectsToAnimate = _objectsToAnimate.Count;
        totalAnimationTime = _objectsToAnimate.Count > 0 
            ? animationDuration + (delayBetweenObjects * (_objectsToAnimate.Count - 1)) 
            : 0f;
        
        _hasInitialized = true;
    }
    
    private bool ShouldExclude(GameObject obj)
    {
        // Direct match in excluded objects list
        if (excludedObjects.Contains(obj))
            return true;
            
        // Check if object is a child of any excluded object
        if (excludeChildren)
        {
            foreach (GameObject excludedObj in excludedObjects)
            {
                if (!excludedObj) continue;
                
                // Check if obj is a child of excludedObj
                Transform parent = obj.transform.parent;
                while (parent)
                {
                    if (parent.gameObject == excludedObj)
                        return true;
                    
                    parent = parent.parent;
                }
            }
        }
        
        // Check for Player exclusion
        if (excludePlayer && TestManager.Instance && TestManager.Instance.Player)
        {
            // Check if the object is the player or a child of the player
            GameObject playerObject = TestManager.Instance.Player.gameObject;
            if (obj == playerObject)
                return true;
                
            Transform parent = obj.transform.parent;
            while (parent)
            {
                if (parent.gameObject == playerObject)
                    return true;
                    
                parent = parent.parent;
            }
        }
        
        // Check for Robot exclusion
        if (excludeRobot && TestManager.Instance && TestManager.Instance.Robot)
        {
            // Check if the object is the robot or a child of the robot
            GameObject robotObject = TestManager.Instance.Robot.gameObject;
            if (obj == robotObject)
                return true;
                
            Transform parent = obj.transform.parent;
            while (parent)
            {
                if (parent.gameObject == robotObject)
                    return true;
                    
                parent = parent.parent;
            }
        }
        
        return false;
    }
    
    private void SortObjectsBasedOnSortMode()
    {
        switch (sortMode)
        {
            case AnimationSortMode.None:
                // Objects remain in the order they were found
                break;
                
            case AnimationSortMode.ByName:
                _objectsToAnimate.Sort((a, b) => String.Compare(a.name, b.name, StringComparison.Ordinal));
                break;
                
            case AnimationSortMode.ByDistance:
                if (!distanceReferencePoint)
                {
                    distanceReferencePoint = transform;
                }
                
                // Sort by distance from reference point
                _objectsToAnimate.Sort((a, b) => {
                    float distA = Vector3.Distance(a.transform.position, distanceReferencePoint.position);
                    float distB = Vector3.Distance(b.transform.position, distanceReferencePoint.position);
                    
                    return sortFromFarthest 
                        ? distB.CompareTo(distA) // Farthest first
                        : distA.CompareTo(distB); // Closest first
                });
                break;
                
            case AnimationSortMode.DistanceFromPlayer:
                if (TestManager.Instance && TestManager.Instance.Player)
                {
                    Transform playerTransform = TestManager.Instance.Player.transform;
                    
                    // Sort by distance from player
                    _objectsToAnimate.Sort((a, b) => {
                        float distA = Vector3.Distance(a.transform.position, playerTransform.position);
                        float distB = Vector3.Distance(b.transform.position, playerTransform.position);
                        
                        return sortFromFarthest 
                            ? distB.CompareTo(distA) // Farthest first
                            : distA.CompareTo(distB); // Closest first
                    });
                }
                else
                {

                }
                break;
                
            case AnimationSortMode.ByHierarchy:
                // Sort by sibling index in the hierarchy
                _objectsToAnimate.Sort((a, b) => {
                    // Get hierarchy paths and compare
                    string pathA = GetHierarchyPath(a.transform);
                    string pathB = GetHierarchyPath(b.transform);
                    return String.Compare(pathA, pathB, StringComparison.Ordinal);
                });
                break;
                
            case AnimationSortMode.Random:
                // Fisher-Yates shuffle
                System.Random rng = new System.Random();
                int n = _objectsToAnimate.Count;
                while (n > 1)
                {
                    n--;
                    int k = rng.Next(n + 1);
                    (_objectsToAnimate[k], _objectsToAnimate[n]) = (_objectsToAnimate[n], _objectsToAnimate[k]);
                }
                break;
                
            case AnimationSortMode.Custom:
                // First, add all objects from custom list that are in objects to animate
                List<GameObject> sortedList = new List<GameObject>();
                
                foreach (GameObject customObj in customOrderedObjects)
                {
                    if (customObj && _objectsToAnimate.Contains(customObj))
                    {
                        sortedList.Add(customObj);
                        // Mark as processed by removing from original list
                        _objectsToAnimate.Remove(customObj);
                    }
                }
                
                // Append any remaining objects that weren't in the custom list
                sortedList.AddRange(_objectsToAnimate);
                
                // Replace the original list with our sorted one
                _objectsToAnimate.Clear();
                _objectsToAnimate.AddRange(sortedList);
                break;
        }
    }
    
    private string GetHierarchyPath(Transform transform)
    {
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
    public void PlayScaleSequence()
    {
        if (!_hasInitialized)
        {
            Initialize();
        }
        else
        {
            // Re-sort objects if already initialized, to ensure we're using the current sort mode
            SortObjectsBasedOnSortMode();
            
            // Update debug values
            numberOfObjectsToAnimate = _objectsToAnimate.Count;
            totalAnimationTime = _objectsToAnimate.Count > 0 
                ? animationDuration + (delayBetweenObjects * (_objectsToAnimate.Count - 1)) 
                : 0f;
        }
        
        // Stop any running sequence
        _animationSequence.Stop();
        _animationSequence = Sequence.Create();
        
        // Set all objects to zero scale
        SetAllObjectsToZeroScale();
        
        // Create the animation sequence
        for (int i = 0; i < _objectsToAnimate.Count; i++)
        {
            GameObject obj = _objectsToAnimate[i];
            if (!obj) continue;
            
            float delay = i * delayBetweenObjects;
            
            _animationSequence.Group(
                Tween.Scale(
                    obj.transform,
                    startValue: Vector3.zero,
                    endValue: _originalScales[obj],
                    animationDuration,
                    ease: scaleEase,
                    startDelay: delay
                )
            );
        }
    }
    
    [Button]
    public void PlayReverseSequence()
    {
        if (!_hasInitialized)
        {
            Initialize();
        }
        else
        {
            // Re-sort objects if already initialized
            SortObjectsBasedOnSortMode();
        }
        
        // Create a temporary reversed list for animation
        List<GameObject> reversedObjects = new List<GameObject>(_objectsToAnimate);
        reversedObjects.Reverse();
        
        // Stop any running sequence
        _animationSequence.Stop();
        _animationSequence = Sequence.Create();
        
        // Ensure all objects are at their original scale before shrinking
        RestoreAllOriginalScales();
        
        // Create the animation sequence - from original scale to zero
        for (int i = 0; i < reversedObjects.Count; i++)
        {
            GameObject obj = reversedObjects[i];
            if (!obj) continue;
            
            float delay = i * delayBetweenObjects;
            
            _animationSequence.Group(
                Tween.Scale(
                    obj.transform,
                    startValue: _originalScales[obj],  // Start from original scale
                    endValue: Vector3.zero,            // Shrink to zero
                    animationDuration,
                    ease: scaleEase,
                    startDelay: delay
                )
            );
        }
        
        // Update debug values
        numberOfObjectsToAnimate = _objectsToAnimate.Count;
        totalAnimationTime = _objectsToAnimate.Count > 0 
            ? animationDuration + (delayBetweenObjects * (_objectsToAnimate.Count - 1)) 
            : 0f;
    }

    private void SetAllObjectsToZeroScale()
    {
        foreach (var obj in _objectsToAnimate)
        {
            if (obj)
            {
                obj.transform.localScale = Vector3.zero;
            }
        }
    }
    
    private void RestoreAllOriginalScales()
    {
        foreach (var obj in _objectsToAnimate)
        {
            if (obj && _originalScales.TryGetValue(obj, out var scale))
            {
                obj.transform.localScale = scale;
            }
        }
    }

    [Button]
    public void ResetAllScales()
    {
        if (!_hasInitialized) return;
        
        // Stop any running sequence
        _animationSequence.Stop();
        
        // Reset all objects to their original scale
        RestoreAllOriginalScales();
    }

    #region Editor Functions

#if UNITY_EDITOR
    [Button]
    private void RefreshObjectList()
    {
        // Reset initialization to force refreshing the object lists
        _hasInitialized = false;
        Initialize();
        Debug.Log($"Found {_objectsToAnimate.Count} objects to animate");
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
        foreach (GameObject obj in _objectsToAnimate)
        {
            if (obj)
            {
                customOrderedObjects.Add(obj);
            }
        }
        
        Debug.Log($"Added {customOrderedObjects.Count} objects to custom order list");
    }
    
#endif

    #endregion
}