using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PositionedBillboard : MonoBehaviour
{
    public Transform centerObject; 
    
    
    [Header("Position Settings")]
    public float movementSmoothTime = 50f; // Lower values = faster movement
    public Vector3 positionOffset = Vector3.up; 
    
    [Header("Rotation Settings")]
    public bool useForwardDirection = true; // If true, forward direction will be based on centerObject's forward
    public float rotationSmoothTime = 0f; // Lower values = faster rotation
    public Vector3 rotationOffset = Vector3.zero; // XYZ Euler angles to offset the rotation
    
    
    // Internal variables
    private Camera _camera;
    private CameraManager _cameraManager;
    private Vector3 _targetPosition;
    private Vector3 _currentRotationVelocity = Vector3.zero;
    
    private void Start()
    {
        if (!_cameraManager) _cameraManager = FindFirstObjectByType<CameraManager>();
        if (!_camera) _camera = Camera.main;
        centerObject = _cameraManager.aimCore.transform;
        
        // Set initial position and rotation
        UpdatePosition(true);
    }
    
    private void Update()
    {
        if (!centerObject || !_camera) return;
            

        UpdatePosition(false);
        

    }

    private void LateUpdate()
    {
        if (!centerObject || !_camera) return;
        
        
        FaceCamera();
    }

    private void UpdatePosition(bool instant)
    {
        if (!centerObject) return;
        
        // Calculate the target position based on centerObject's transform
        Vector3 offsetDirection = positionOffset;
        
        // Apply the offset in world space if using player's transform as reference
        if (useForwardDirection)
        {
            // Transform the offset direction from local to world space
            offsetDirection = centerObject.TransformDirection(positionOffset);
        }
        
        // Set height separately to keep it independent of rotation
        Vector3 worldOffset = offsetDirection;
        worldOffset.y = 0; // Zero out y component
        
        // Calculate the final position with height offset
        _targetPosition = centerObject.position + worldOffset + new Vector3(0, positionOffset.y, 0);
        
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
        
        if (centerObject)
        {
            UpdatePosition(true);
        }
        else
        {
            if (!_cameraManager) _cameraManager = FindFirstObjectByType<CameraManager>();
            if (!_camera) _camera = Camera.main;
            centerObject = _cameraManager.aimCore.transform;
        }
    }
#endif
}