using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PositionedBillboard : MonoBehaviour
{
    [Header("Position Settings")]
    public float movementSmoothTime = 50f; // Lower values = faster movement
    public Vector3 positionOffset = Vector3.up;
    public bool maintainRelativeToCameraView = true; // NEW: Keep position relative to camera view
    
    [Header("Rotation Settings")]
    public bool useForwardDirection = true; // If true, forward direction will be based on parent's forward
    public float rotationSmoothTime = 0f; // Lower values = faster rotation
    public Vector3 rotationOffset = Vector3.zero; // XYZ Euler angles to offset the rotation
    
    // Internal variables
    private Camera _camera;
    private Transform _parentTransform;
    private Vector3 _targetPosition;
    private Vector3 _currentRotationVelocity = Vector3.zero;
    
    private void Start()
    {
        if (!_camera) _camera = Camera.main;
        _parentTransform = transform.parent;
        
        // Set initial position and rotation
        UpdatePosition(true);
    }
    
    private void LateUpdate()
    {
        if (!_parentTransform || !_camera) return;
        
        UpdatePosition(false);
        FaceCamera();
    }

    private void UpdatePosition(bool instant)
    {
        if (!_parentTransform || !_camera) return;
        
        Vector3 offsetDirection;
        
        if (maintainRelativeToCameraView)
        {
            // Calculate offset based on camera's view direction
            // This makes the billboard stay on the same side relative to the camera
            Vector3 cameraForward = _camera.transform.forward;
            cameraForward.y = 0; // Keep it horizontal
            cameraForward.Normalize();
            
            // Get camera right vector (perpendicular to forward)
            Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraForward).normalized;
            
            // Calculate position based on camera orientation
            // Example: positionOffset.x controls right/left, positionOffset.z controls forward/back
            offsetDirection = (cameraRight * positionOffset.x) + (cameraForward * positionOffset.z);
            
            // Add vertical offset
            offsetDirection += Vector3.up * positionOffset.y;
        }
        else
        {
            // Use original positioning logic
            offsetDirection = positionOffset;
            
            // Apply the offset in world space if using player's transform as reference
            if (useForwardDirection)
            {
                // Transform the offset direction from local to world space
                offsetDirection = _parentTransform.TransformDirection(positionOffset);
            }
        }
        
        // Calculate the final position
        _targetPosition = _parentTransform.position + offsetDirection;
        
        // Apply smoothing or set instantly
        if (instant)
        {
            transform.position = _targetPosition;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * movementSmoothTime);
        }
    }
    
    private void FaceCamera()
    {
        if (!_camera) return;
        
        // Get direction to camera
        Vector3 lookDirection = _camera.transform.position - transform.position;
        
        // Create a locked direction that ignores vertical component
        Vector3 flatLookDirection = new Vector3(lookDirection.x, 0, lookDirection.z);
        
        // Create the base rotation (looking at camera horizontally)
        Quaternion baseRotation = Quaternion.LookRotation(flatLookDirection);
        
        // Get the target euler angles
        Vector3 targetEuler = baseRotation.eulerAngles;
        
        // Apply the rotation offset
        targetEuler += rotationOffset;
        
        // Current rotation in euler angles
        Vector3 currentEuler = transform.rotation.eulerAngles;
        
        // Smooth the Y rotation (horizontal)
        float smoothedY = Mathf.SmoothDampAngle(
            currentEuler.y, 
            targetEuler.y, 
            ref _currentRotationVelocity.y, 
            rotationSmoothTime
        );
        
        // Smooth the X rotation (vertical tilt)
        float smoothedX = Mathf.SmoothDampAngle(
            currentEuler.x, 
            targetEuler.x + rotationOffset.x, 
            ref _currentRotationVelocity.x, 
            rotationSmoothTime
        );
        
        // Smooth the Z rotation (roll)
        float smoothedZ = Mathf.SmoothDampAngle(
            currentEuler.z, 
            targetEuler.z + rotationOffset.z, 
            ref _currentRotationVelocity.z, 
            rotationSmoothTime
        );
        
        // Apply the new rotation with all three axes
        transform.rotation = Quaternion.Euler(smoothedX, smoothedY, smoothedZ);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || SceneManager.GetActiveScene().buildIndex != 0) return;
        
        if (!_parentTransform) _parentTransform = transform.parent;
        if (!_camera) _camera = Camera.main;
        
        if (_parentTransform && _camera)
        {
            UpdatePosition(true);
        }
    }
#endif
}