using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[RequireComponent(typeof(Interactable))]
public class PressurePlate : MonoBehaviour
{
    [Header("Pressure Plate Settings")]
    [SerializeField] private Transform plateTransform;            
    [SerializeField] private float plateHeight = 0.1f;            
    [SerializeField] private float plateAnimationSpeed = 5f;         

    [Header("Pressure Plate Events")]
    [SerializeField] private UnityEvent onPlateActivated;           
    [SerializeField] private UnityEvent onPlateDeactivated;         
    
    private Vector3 _initialPlatePosition;                    
    private Vector3 _pressedPlatePosition;                     
    private bool _isActivated = false;                            
    private readonly HashSet<GameObject> _objectsOnPlate = new HashSet<GameObject>(); 
    private RobotCompanion _robot;
    private Interactable _interactable;
    
    private void Awake()
    {
        // Get the Interactable component
        _interactable = GetComponent<Interactable>();
        
        // Find the moving plate part (first child by default)
        if (!plateTransform)
        {
            plateTransform = transform.GetChild(0);
        }
        
        // Store initial positions
        _initialPlatePosition = plateTransform.localPosition;
        _pressedPlatePosition = _initialPlatePosition - new Vector3(0, plateHeight, 0);
        
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
    
    private void OnTriggerEnter(Collider other)
    {
        // Add to tracking set
        _objectsOnPlate.Add(other.gameObject);
        
        if (other.TryGetComponent(out RobotCompanion robot))
        {
            _robot = robot;
        }
            
        // Activate if not already activated
        if (!_isActivated)
        {
            Activate();
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Remove from tracking set
        _objectsOnPlate.Remove(other.gameObject);
        

        if (other.TryGetComponent(out RobotCompanion robot) && robot == _robot)
        {
            _robot = null;
        }
        
        // If no objects left on plate, deactivate
        if (_objectsOnPlate.Count == 0 && _isActivated)
        {
            Deactivate();
        }
    }

    private void Activate()
    {
        if (_isActivated) return;

        _isActivated = true;
        onPlateActivated?.Invoke();
    }
    
    private void Deactivate()
    {
        if (!_isActivated) return;

        _isActivated = false;
        onPlateDeactivated?.Invoke();
    }


    private void OnInteractionStart()
    {

    }
    

    private void OnInteractionEnd()
    {
        if (_robot)
        {
            _robot.CommandSitDown();
            _robot = null;
        }
        
    }
}