using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

public enum IKTarget
{
    Robot,
    Interactable,
    CameraAimDir,
    Menu,
    None
}

[RequireComponent(typeof(PlayerStateMachine))]
public class PlayerIKHandler : MonoBehaviour
{
    [Header("IK")]
    [SerializeField] private Transform ikTarget;
    [SerializeField] private Rig rig;
    
    [Header("Head")]
    [SerializeField] private MultiAimConstraint headIK;
    [SerializeField, Range(0, 1)] private float maxHeadWeight = 0.7f;
    [SerializeField, Min(0.1f)] private float headIKSmoothTime = 2f;
    
    [Header("Spine")]
    [SerializeField] private MultiAimConstraint spineIK;
    [SerializeField, Range(0, 1)] private float maxSpineWeight = 0.7f;
    [SerializeField, Min(0.1f)] private float spineIKSmoothTime = 2f;
    

    [Header("IK Parameters")]
    [SerializeField] private float robotDistanceThreshold = 2f;
    [SerializeField] private float interactableDistanceThreshold = 1f;
    [SerializeField] private float ikSmoothSpeed = 5f;
    
    private PlayerStateMachine _stateMachine;
    private Camera _camera;
    private float _targetHeadWeight = 1f;
    private float _targetSpineWeight = 1f;
    private Vector3 _currentIKPosition;
    private IKTarget _currentIKTarget = IKTarget.None;

    private void Awake()
    {
        _stateMachine = GetComponent<PlayerStateMachine>();
    }

    private void Start()
    {
        if (!_camera) _camera = Camera.main;
    }

    private void Update()
    {
        DetermineCurrentIKTarget();
        UpdateIKTarget();
        UpdateHeadIK();
        UpdateSpineIK();
    }
    
    #region IK -------------------------------------------------------------------------------------------------------

    private void DetermineCurrentIKTarget()
    {
        if (_stateMachine.CurrentState == _stateMachine.InMenuState)
        {
            _currentIKTarget = IKTarget.Menu;
            return;
        }
        
        if (_stateMachine.robot && !_stateMachine.IsAiming && false)
        {
            float distanceToRobot = Vector3.Distance(transform.position, _stateMachine.robot.transform.position);
            if (distanceToRobot < robotDistanceThreshold)
            {
                _currentIKTarget = IKTarget.Robot;
                return;
            }
        }
        
        if (_stateMachine.CurrentInteractable && !_stateMachine.IsAiming)
        {
            float distanceToInteractable= Vector3.Distance(transform.position, _stateMachine.CurrentInteractable.transform.position);
            if (distanceToInteractable < interactableDistanceThreshold)
            {
                _currentIKTarget = IKTarget.Interactable;
                return;
            }
            return;
        }
        
        if (_stateMachine.cameraManager)
        {
            _currentIKTarget = IKTarget.CameraAimDir;
            return;
        }
        
        _currentIKTarget = IKTarget.None;
    }

    private void UpdateIKTarget()
    {
        if (!ikTarget) return;
        
        Vector3 targetPosition = GetTargetPositionForCurrentIKTarget();
        
        // Only update if we have a valid target
        if (_currentIKTarget != IKTarget.None)
        {
            // Lerp the position
            _currentIKPosition = Vector3.Lerp(_currentIKPosition, targetPosition, Time.deltaTime * ikSmoothSpeed);
            
            // Apply the lerped position
            ikTarget.transform.position = _currentIKPosition;
        }
    }
    
    
    private void UpdateSpineIK()
    {
        if (!rig || !spineIK) return;
        
        bool allowedState = _stateMachine.CurrentState != _stateMachine.InMenuState && 
                            _stateMachine.CurrentState != _stateMachine.CrouchingState && 
                            _stateMachine.CurrentState != _stateMachine.JumpingState && 
                            _stateMachine.CurrentState != _stateMachine.FallingState &&
                            Mathf.Abs(_stateMachine.ActiveHorizontalVelocity) < _stateMachine.runSpeed &&
                            _currentIKTarget != IKTarget.Robot;

        if (allowedState)
        {
            spineIK.weight = UpdateIKWeight(_targetSpineWeight, spineIK.weight, maxSpineWeight, spineIKSmoothTime);
        }
        else
        {
            spineIK.weight = Mathf.MoveTowards(spineIK.weight, 0f, Time.deltaTime * spineIKSmoothTime / 3);
        }
    }

    private void UpdateHeadIK()
    {
        if (!rig || !headIK) return;
        
        bool allowedState = _stateMachine.CurrentState != _stateMachine.CrouchingState && 
                            _stateMachine.CurrentState != _stateMachine.FallingState &&
                            Mathf.Abs(_stateMachine.ActiveHorizontalVelocity) < _stateMachine.runSpeed + 0.5f &&
                            _currentIKTarget != IKTarget.None;
        
        if (allowedState)
        {
            headIK.weight = UpdateIKWeight(_targetHeadWeight, headIK.weight, maxHeadWeight, headIKSmoothTime);
        }
        else
        {
            headIK.weight = Mathf.MoveTowards(headIK.weight, 0f, Time.deltaTime * headIKSmoothTime);
        }
    }
    
    
    


    #endregion IK -------------------------------------------------------------------------------------------------------


    #region Helper methods -------------------------------------------------------------------------------------------------------

    private Vector3 GetTargetPositionForCurrentIKTarget()
    {
        switch (_currentIKTarget)
        {
            case IKTarget.Robot:
                return _stateMachine.robot.transform.position;
                
            case IKTarget.Interactable:
                return _stateMachine.CurrentInteractable.GetInteractPosition(_stateMachine).position;
                
            case IKTarget.CameraAimDir:
                return _stateMachine.cameraManager.targetTransform.position + new Vector3(0, 0.2F, 0);
                
            case IKTarget.Menu:
                // Get mouse position in screen coordinates
                Vector3 mousePos = Input.mousePosition;
            
                // Create a ray from the camera through the mouse position
                Ray ray = _camera.ScreenPointToRay(mousePos);
                
                // Default position will be a point along the ray
                return ray.origin + ray.direction * 10;
                
            case IKTarget.None:
            default:
                return _currentIKPosition; // Return current position to avoid sudden jumps
        }
    }
    private float UpdateIKWeight(float targetWeight, float currentWeight, float maxWeight, float smoothTime)
    {
        // Determine target weight based on rotation mismatch
        float rotationThreshold = _currentIKTarget == IKTarget.CameraAimDir ? 140 : 180;
        
        if (!_stateMachine.IsAiming && Mathf.Abs(_stateMachine.RotationMismatch) > Mathf.Abs(rotationThreshold))
        {
            targetWeight = 0f;
        }
        else
        {
            targetWeight = maxWeight;
        }

        // Smoothly interpolate the actual weight toward the target weight
        return Mathf.MoveTowards(currentWeight, targetWeight, Time.deltaTime * smoothTime);
    }

    #endregion Helper methods -------------------------------------------------------------------------------------------------------
    

}