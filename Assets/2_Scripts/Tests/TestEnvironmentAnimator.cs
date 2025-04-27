using System;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;
using VInspector;


public class TestEnvironmentAnimator : MonoBehaviour
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
    [SerializeField] private bool findAllAnimatedObjectsInScene = true;
    [SerializeField] private List<GameObject> additionalObjectsToAnimate = new List<GameObject>();
    
    [Header("Debug")] 
    [SerializeField, ReadOnly] private float totalAnimationTime; 
    [SerializeField, ReadOnly] private int numberOfObjectsToAnimate;
    [SerializeField, ReadOnly] private  List<GameObject> objectsToAnimate = new List<GameObject>();


    private readonly Vector3 _defaultPlayerSize = new Vector3(1,1,1);
    private readonly Dictionary<GameObject, Vector3> _originalScales = new Dictionary<GameObject, Vector3>();
    private bool IsDistanceSort => sortMode == AnimationSortMode.ByDistance || sortMode == AnimationSortMode.DistanceFromPlayer;
    private bool _hasInitialized = false;
    private Sequence _animationSequence;
    private TestManager _testManager;
    private GameObject _floorGameObject;
    private GameObject _playerGameObject;
    private GameObject _robotGameObject;
    private enum AnimationSortMode
    {
        None,           // Use order objects are found in scene
        ByName,         // Sort alphabetically by name
        ByDistance,     // Sort by distance from a reference point
        ByHierarchy,    // Sort by hierarchy order in scene
        Random,         // Randomize the order
        Custom,         // Use a custom ordered list
        DistanceFromPlayer  // Sort by distance from the player
    }
    public bool PlayOnTestLoading => playOnTestLoading;
    public bool PlayOnTestUnloading => playOnTestUnloading;
    public float TotalAnimationTime => totalAnimationTime;

    private void Awake()
    {
        _testManager = GetComponent<TestManager>();
        
        Initialize();
    }

    private void OnEnable()
    {
        _testManager?.onIntroSequenceStart.AddListener(OnIntroSequenceStart);
    }

    private void OnDisable()
    {
        _testManager?.onIntroSequenceStart.RemoveListener(OnIntroSequenceStart);
        
        if (resetOnDisable)
        {
            ResetAllScales();
        }
    }
    

    private void OnIntroSequenceStart()
    {
        if (!_testManager) return;
        
        PlayIntroSequence(_testManager.IntroSequenceDuration*1.5f);
    }
    
    private void Initialize()
    {
        if (_hasInitialized) return;
        
        objectsToAnimate.Clear();
        _originalScales.Clear();

        
        // Check for separate objects
        _floorGameObject = GameObject.Find("Floor");
        _playerGameObject = GameObject.Find("Player");
        _robotGameObject = GameObject.Find("Robot");
        StoreSpecialObjectScale(_floorGameObject);
        StoreSpecialObjectScale(_playerGameObject);
        StoreSpecialObjectScale(_robotGameObject);
        
        
        // Find all TestAnimatedObject components in the scene if option is enabled
        if (findAllAnimatedObjectsInScene)
        {
            TestAnimatedObject[] allAnimatedObjects = FindObjectsByType<TestAnimatedObject>(FindObjectsSortMode.None);
            foreach (TestAnimatedObject animObj in allAnimatedObjects)
            {
                // Only include objects that have AffectedByTestAnimations set to true
                if (animObj.AffectedByTestAnimations)
                {
                    objectsToAnimate.Add(animObj.gameObject);
                    _originalScales[animObj.gameObject] = animObj.transform.localScale;
                }
            }
        }
        
        // Add any additional objects
        foreach (GameObject obj in additionalObjectsToAnimate)
        {
            if (obj && !objectsToAnimate.Contains(obj))
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
    

    private void StoreSpecialObjectScale(GameObject specialObject)
    {
        if (specialObject)
        {
            // Check if it has a TestAnimatedObject component
            TestAnimatedObject animObj = specialObject.GetComponent<TestAnimatedObject>();
        
            // Only store the scale if either it has the component and AffectedByTestAnimations is true,
            // or we want to animate it regardless of the component
            if ((animObj && animObj.AffectedByTestAnimations) || !animObj)
            {
                _originalScales[specialObject] = specialObject.transform.localScale;
            }
        }
    }
    
    private void SortObjectsBasedOnSortMode()
    {
        // First, remove any null or destroyed objects from the list
        objectsToAnimate.RemoveAll(obj => !obj);
        
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
                    objectsToAnimate.Sort((objectA, objectB) => {
                        if (!objectA || !objectB) return 0;
                        
                        try {
                            float distA = Vector3.Distance(objectA.transform.position, playerTransform.position);
                            float distB = Vector3.Distance(objectB.transform.position, playerTransform.position);
                            
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
                objectsToAnimate.Sort((objectA, objectB) => {
                    if (!objectA || !objectB) return 0;
                    
                    try {
                        // Get hierarchy paths and compare
                        string pathA = objectA.transform.GetHierarchyPath();
                        string pathB = objectB.transform.GetHierarchyPath();
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
        
        // Remove special objects from the animation list
        RemoveSpecialObjectsFromAnimationList(objectsToAnimate);
    }
    
    private void RemoveSpecialObjectsFromAnimationList(List<GameObject> list)
    {
        // Remove floor, player and robot from the list if they exist
        if (_floorGameObject && list.Contains(_floorGameObject))
        {
            list.Remove(_floorGameObject);
        }
    
        if (_playerGameObject && list.Contains(_playerGameObject))
        {
            list.Remove(_playerGameObject);
        }
    
        if (_robotGameObject && list.Contains(_robotGameObject))
        {
            list.Remove(_robotGameObject);
        }
    }
    
    private bool ShouldAnimateSpecialObject(GameObject obj)
    {
        if (!obj) return false;
    
        // Check if we've stored its original scale (means it passed our earlier checks)
        if (!_originalScales.ContainsKey(obj)) return false;
    
        // Get TestAnimatedObject component if it exists
        TestAnimatedObject animObj = obj.GetComponent<TestAnimatedObject>();
    
        // If it has the component, respect its AffectedByTestAnimations setting
        // If it doesn't have the component, animate it anyway
        return !animObj || animObj.AffectedByTestAnimations;
    }

    [Button]
    public void PlayLoadSequence(float animationTime)
    {
        if (!_hasInitialized)
        {
            Initialize();
        }
        else
        {
            // Clean up object list before sorting
            objectsToAnimate.RemoveAll(obj => !obj);
            
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
        SetAllObjectsToZeroScale(true);
    
        // Calculate individual animation durations and delays
        float totalDelayTime = objectsToAnimate.Count > 1 
            ? animationTime * delayTimeFactor // portion of time used for delays
            : 0f;
    
        float individualDuration = objectsToAnimate.Count > 1 
            ? animationTime * (1f - delayTimeFactor) // remaining portion for actual animation
            : animationTime;
        
        
        // Create an animation only for the floor object
        if (_floorGameObject && ShouldAnimateSpecialObject(_floorGameObject))
        {
            _animationSequence.Group(
                Tween.Scale(
                    _floorGameObject.transform,
                    startValue: Vector3.zero,
                    endValue: _originalScales[_floorGameObject],
                    animationTime,
                    ease: scaleUpEase
                )
            );
        }
        
        // Create an animation only for the player object
        if (_playerGameObject && ShouldAnimateSpecialObject(_playerGameObject))
        {
            _animationSequence.Group(
                Tween.Scale(
                    _playerGameObject.transform,
                    startValue: Vector3.zero,
                    endValue: _defaultPlayerSize,
                    animationTime,
                    ease: scaleUpEase
                )
            );
        }

        
        // Create an animation only for the robot object
        if (_robotGameObject && ShouldAnimateSpecialObject(_robotGameObject))
        {
            _animationSequence.Group(
                Tween.Scale(
                    _robotGameObject.transform,
                    startValue: Vector3.zero,
                    endValue: _originalScales[_robotGameObject],
                    animationTime,
                    ease: scaleUpEase
                )
            );
        }
        
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
    public void PlayUnLoadSequence(float animationTime)
    {
        if (!_hasInitialized)
        {
            Initialize();
        }
        else
        {
            // Clean up object list before sorting
            objectsToAnimate.RemoveAll(obj => !obj);
            
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

        
        // Create an animation only for the floor object
        if (_floorGameObject && ShouldAnimateSpecialObject(_floorGameObject))
        {
            _animationSequence.Group(
                Tween.Scale(
                    _floorGameObject.transform,
                    startValue: _originalScales[_floorGameObject],
                    endValue: Vector3.zero,
                    animationTime,
                    ease: scaleDownEase
                )
            );
        }
        
        // Create an animation only for the player object
        if (_playerGameObject && ShouldAnimateSpecialObject(_playerGameObject))
        {
            _animationSequence.Group(
                Tween.Scale(
                    _playerGameObject.transform,
                    startValue: _originalScales[_playerGameObject],
                    endValue: Vector3.zero,
                    animationTime,
                    ease: scaleDownEase
                )
            );
        }
        
        // Create an animation only for the robot object
        if (_robotGameObject && ShouldAnimateSpecialObject(_robotGameObject))
        {
            _animationSequence.Group(
                Tween.Scale(
                    _robotGameObject.transform,
                    startValue: _originalScales[_robotGameObject],
                    endValue: Vector3.zero,
                    animationTime,
                    ease: scaleDownEase
                )
            );
        }
        
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

       
        
        numberOfObjectsToAnimate = objectsToAnimate.Count;
        totalAnimationTime = animationTime; 
    }
    
    private void PlayIntroSequence(float animationTime)
    {
        // Clean up object list before sorting
        objectsToAnimate.RemoveAll(obj => !obj);
            
        // Refresh original scales in case objects have changed
        foreach (GameObject obj in objectsToAnimate)
        {
            if (obj && !_originalScales.ContainsKey(obj))
            {
                _originalScales[obj] = obj.transform.localScale;
            }
        }
        
        // sort objects to animate farthest from the player first
        if (_testManager && _testManager.Player)
        {
            Transform playerTransform = _testManager.Player.transform;
            
            // Sort by distance from player
            objectsToAnimate.Sort((objectA, objectB) => {
                if (!objectA || !objectB) return 0;
                
                try {
                    float distA = Vector3.Distance(objectA.transform.position, playerTransform.position);
                    float distB = Vector3.Distance(objectB.transform.position, playerTransform.position);
                    
                    return distB.CompareTo(distA); // Farthest first
                }
                catch (Exception) {
                    // If any exception occurs, consider the objects equal
                    return 0;
                }
            });
        }
        else
        {
            SortObjectsBasedOnSortMode();
        }
        
        
    
        // Update debug values
        numberOfObjectsToAnimate = objectsToAnimate.Count;
        totalAnimationTime = animationTime; // Total time is exactly animationTime

        // Stop any running sequence
        _animationSequence.Stop();
        _animationSequence = Sequence.Create();

        // Set all objects to zero scale
        SetAllObjectsToZeroScale(false);
    
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
    


    public void SetAllObjectsToZeroScale(bool scaleSpecialObjects)
    {
        foreach (var obj in objectsToAnimate)
        {
            if (obj)
            {
                obj.transform.localScale = Vector3.zero;
            }
        }
        

        if (scaleSpecialObjects)
        {
            if (_floorGameObject && ShouldAnimateSpecialObject(_floorGameObject))
            {
                _floorGameObject.transform.localScale = Vector3.zero;
            }
            
            if (_playerGameObject && ShouldAnimateSpecialObject(_playerGameObject))
            {
                _playerGameObject.transform.localScale = Vector3.zero;
            }
            
            if (_robotGameObject && ShouldAnimateSpecialObject(_robotGameObject))
            {
                _robotGameObject.transform.localScale = Vector3.zero;
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
        
        // Restore the special objects to their original scales
        if (_floorGameObject && ShouldAnimateSpecialObject(_floorGameObject))
        {
            _floorGameObject.transform.localScale = _originalScales[_floorGameObject];
        }
        
        if (_playerGameObject && ShouldAnimateSpecialObject(_playerGameObject))
        {
            _playerGameObject.transform.localScale = _defaultPlayerSize;
        }
        
        if (_robotGameObject && ShouldAnimateSpecialObject(_robotGameObject))
        {
            _robotGameObject.transform.localScale = _originalScales[_robotGameObject];
        }
    }


    public void RefreshForNewEnvironment()
    {
        _hasInitialized = false;
        Initialize();
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
    
    
    #region Editor Functions

#if UNITY_EDITOR
    
    [ContextMenu("Refresh Object List")]
    private void RefreshObjectList()
    {
        // Reset initialization to force refreshing the object lists
        _hasInitialized = false;
        Initialize();
        Debug.Log($"Found {objectsToAnimate.Count} objects to animate");
    }
    
    [ContextMenu("Fill Custom Order From Current")]
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
    
    [ContextMenu("Reverse Animation Order")]
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