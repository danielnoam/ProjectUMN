using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.Serialization;

[SelectionBase]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Interactable))]
public class PressurePlate : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector3 checkBoxOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 checkBoxSize = new Vector3(1f, 0.5f, 1f);
    [SerializeField] private LayerMask interactableLayer;
    
    [Header("Feedback")]
    [SerializeField] private Transform plateTransform;            
    [SerializeField] private float plateAnimationHeight = 0.1f;            
    [SerializeField] private float plateAnimationSpeed = 8f;
    [SerializeField] private SOAudioEvent sfxPlatePress;
    [SerializeField] private SOAudioEvent sfxPlateRelease;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onPlateActivated;           
    [SerializeField] private UnityEvent onPlateDeactivated;         
    
    private Vector3 _initialPlatePosition;                    
    private Vector3 _pressedPlatePosition;                     
    private bool _isActivated = false;                            
    private readonly HashSet<GameObject> _objectsOnPlate = new HashSet<GameObject>(); 
    private Interactable _interactable;
    private AudioSource _audioSource;
    
    private void Awake()
    {
        // Get the Interactable component
        _interactable = GetComponent<Interactable>();
        _audioSource = GetComponent<AudioSource>();
        
        // Find the moving plate part (first child by default)
        if (!plateTransform)
        {
            plateTransform = transform.GetChild(0);
        }
        
        // Store initial positions
        _initialPlatePosition = plateTransform.localPosition;
        _pressedPlatePosition = _initialPlatePosition - new Vector3(0, plateAnimationHeight, 0);
        
        // Subscribe to Interactable events
        _interactable.onInteractStartEvents.AddListener(OnInteractionStart);
        _interactable.onInteractEndEvents.AddListener(OnInteractionEnd);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (_interactable != null)
        {
            _interactable.onInteractStartEvents.RemoveListener(OnInteractionStart);
            _interactable.onInteractEndEvents.RemoveListener(OnInteractionEnd);
        }
    }
    
    private void Update()
    {
        // Animate plate position based on activation state
        Vector3 targetPosition = _isActivated ? _pressedPlatePosition : _initialPlatePosition;
        plateTransform.localPosition = Vector3.Lerp(
            plateTransform.localPosition,
            targetPosition,
            Time.deltaTime * plateAnimationSpeed
        );
    }
    
    private void FixedUpdate()
    {
        CheckForObjectsOnPlate();
    }
    

    private void CheckForObjectsOnPlate()
    {
        // Clear previous objects
        _objectsOnPlate.Clear();
        
        // Check for colliders in the plate area
        Collider[] collidersOnPlate = Physics.OverlapBox(
            transform.position + checkBoxOffset, 
            checkBoxSize * 0.5f, 
            transform.rotation, 
            interactableLayer
        );
        
        // Process detected objects
        foreach (Collider col in collidersOnPlate)
        {
            _objectsOnPlate.Add(col.gameObject);
        }
        
        // Update activation state
        if (_objectsOnPlate.Count > 0 && !_isActivated)
        {
            Activate();
        }
        else if (_objectsOnPlate.Count == 0 && _isActivated)
        {
            Deactivate();
        }
    }

    private void Activate()
    {
        if (_isActivated) return;

        _isActivated = true;
        sfxPlatePress?.Play(_audioSource);
        onPlateActivated?.Invoke();
    }
    
    private void Deactivate()
    {
        if (!_isActivated) return;

        _isActivated = false;
        sfxPlateRelease?.Play(_audioSource);
        onPlateDeactivated?.Invoke();
    }

    private void OnInteractionStart()
    {

    }
    
    private void OnInteractionEnd()
    {

    }



#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero + checkBoxOffset, checkBoxSize);
    }
#endif

}