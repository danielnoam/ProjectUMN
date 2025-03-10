using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;



[RequireComponent(typeof(PlayerStateMachine))]
public class PlayerIKHandler : MonoBehaviour
{
    
    [Header("IK")]
    [SerializeField] private Transform IKTarget;
    [SerializeField] private Rig rig;
    
    [Header("Head")]
    [SerializeField] private MultiAimConstraint headIK;
    [SerializeField, Range(0, 1)] private float maxHeadWeight = 0.7f;
    [FormerlySerializedAs("headIKSmoothing")] [SerializeField, Min(0.1f)] private float headIKSmoothTime = 2f;
    
    
    [Header("Spine")]
    [SerializeField] private MultiAimConstraint spineIK;
    [SerializeField, Range(0, 1)] private float maxSpineWeight = 0.7f;
    [FormerlySerializedAs("spineIKSmoothing")] [SerializeField, Min(0.1f)] private float spineIKSmoothTime = 2f;
    
    private PlayerStateMachine _stateMachine;
    private CameraManager _cameraManager;
    private Camera _camera;
    private float _targetHeadWeight = 1f;
    private float _targetSpineWeight = 1f;
    private Vector3 _currentIKPosition;

    private void Awake()
    {
        _stateMachine = GetComponent<PlayerStateMachine>();
    }

    private void Start()
    {
        if (!_cameraManager) _cameraManager = FindFirstObjectByType<CameraManager>();
        if (!_camera) _camera = Camera.main;
    }

    private void Update()
    {
        UpdateIKTarget();
        UpdateHeadIK();
        UpdateSpineIK();
    }

    
    
    
    #region IK -------------------------------------------------------------------------------------------------------

    private void UpdateIKTarget()
    {
        if (!IKTarget) return;
        
        Vector3 targetPosition;
        
        
        if (_stateMachine.CurrentState != _stateMachine.InMenuState)
        {
            if (_stateMachine.CurrentInteractable && !_stateMachine.IsAiming)
            {
                targetPosition = _stateMachine.CurrentInteractable.GetInteractPosition(_stateMachine).position;
            }
            else if (_cameraManager)
            {
                targetPosition = _cameraManager.targetTransform.position;
            }
            else
            {
                return;
            }
        }
        else
        {
            // Get mouse position in screen coordinates
            Vector3 mousePos = Input.mousePosition;
        
            // Create a ray from the camera through the mouse position
            Ray ray = _camera.ScreenPointToRay(mousePos);
            
            // Default position will be a point along the ray
            targetPosition = ray.origin + ray.direction * 10;
        }
        
        // Lerp the position
        _currentIKPosition = Vector3.Lerp(_currentIKPosition, targetPosition, Time.deltaTime * 5);
        
        // Apply the lerped position
        IKTarget.transform.position = _currentIKPosition;
    }
    
    private void UpdateSpineIK()
    {
        if (!rig || !spineIK) return;
        
        bool allowedState = _stateMachine.CurrentState != _stateMachine.InMenuState && _stateMachine.CurrentState != _stateMachine.CrouchingState && _stateMachine.CurrentState != _stateMachine.FallingState;

        if (allowedState && _stateMachine.ActiveHorizontalVelocity < _stateMachine.runSpeed - 2f)
        {
            if (_stateMachine.CurrentInteractable && !_stateMachine.IsAiming)
            {
            
                // Determine target weight based on conditions
                if (!_stateMachine.IsAiming && Mathf.Abs(_stateMachine.RotationMismatch) > Mathf.Abs(180))
                {
                    _targetSpineWeight = 0f;
                }
                else
                {
                    _targetSpineWeight = maxSpineWeight;
                }

                // Smoothly interpolate the actual weight toward the target weight
                spineIK.weight = Mathf.MoveTowards(spineIK.weight, _targetSpineWeight, Time.deltaTime * spineIKSmoothTime);
            }
            else if (_cameraManager)
            {
            
                // Determine target weight based on conditions
                if (!_stateMachine.IsAiming && Mathf.Abs(_stateMachine.RotationMismatch) > Mathf.Abs(140))
                {
                    _targetSpineWeight = 0f;
                }
                else
                {
                    _targetSpineWeight = maxSpineWeight;
                }

                // Smoothly interpolate the actual weight toward the target weight
                spineIK.weight = Mathf.MoveTowards(spineIK.weight, _targetSpineWeight, Time.deltaTime * spineIKSmoothTime);
            }
        }
        else
        {
            spineIK.weight = Mathf.MoveTowards(spineIK.weight, 0f, Time.deltaTime * spineIKSmoothTime / 2);
        }
    }

    private void UpdateHeadIK()
    {
        if (!rig || !headIK) return;

        if (_stateMachine.CurrentInteractable && !_stateMachine.IsAiming)
        {
            
            // Determine target weight based on conditions
            if (!_stateMachine.IsAiming && Mathf.Abs(_stateMachine.RotationMismatch) > Mathf.Abs(180))
            {
                _targetHeadWeight = 0f;
            }
            else
            {
                _targetHeadWeight = maxHeadWeight;
            }

            // Smoothly interpolate the actual weight toward the target weight
            headIK.weight = Mathf.MoveTowards(headIK.weight, _targetHeadWeight, Time.deltaTime * headIKSmoothTime);
        }
        else if (_cameraManager)
        {
            
            // Determine target weight based on conditions
            if (!_stateMachine.IsAiming && Mathf.Abs(_stateMachine.RotationMismatch) > Mathf.Abs(140))
            {
                _targetHeadWeight = 0f;
            }
            else
            {
                _targetHeadWeight = maxHeadWeight;
            }

            // Smoothly interpolate the actual weight toward the target weight
            headIK.weight = Mathf.MoveTowards(headIK.weight, _targetHeadWeight, Time.deltaTime * headIKSmoothTime);
        }
        else
        {
            headIK.weight = Mathf.MoveTowards(headIK.weight, 0f, Time.deltaTime * headIKSmoothTime / 3);
        }
    }
    

    #endregion IK -------------------------------------------------------------------------------------------------------
    
}