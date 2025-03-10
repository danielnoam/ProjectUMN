using System;
using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    

    [Header("Rotation")]
    [SerializeField] private bool rotateX = true;
    [SerializeField] private bool rotateY = true;
    [SerializeField] private bool rotateZ = false;
    [SerializeField] private bool smoothRotation = true;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    
    [Header("Distance Scaling")]
    [SerializeField] private bool scaleWithDistance = true;
    [SerializeField] private bool smoothScaling = true;
    [SerializeField] private float minDistance = 7f;
    [SerializeField] private float maxDistance = 18f;
    [SerializeField] private float scalingSpeed = 10f;
    

    private Camera _targetCamera;
    private Vector3 _originalScale;
    private float _originalXRotation;
    private float _originalZRotation;
    

    private void Awake()
    {
        // Store the original rotations
        Vector3 currentRotation = transform.rotation.eulerAngles;
        _originalXRotation = currentRotation.x;
        _originalZRotation = currentRotation.z;
        
        // Store the original scale
        _originalScale = transform.localScale;
    }

    private void Start()
    {
        // If no camera is assigned, use the main camera
        if (!_targetCamera)
        {
            _targetCamera = Camera.main;
        }
    }
    
    private void LateUpdate()
    {
        if (!_targetCamera) return;
            
        // Get the direction from the object to the camera
        Vector3 directionToCamera = _targetCamera.transform.position - transform.position;
        
        // Calculate distance to camera for scaling
        float distanceToCamera = directionToCamera.magnitude;
        
        // Create a rotation that looks at the camera
        Quaternion lookRotation = Quaternion.LookRotation(directionToCamera);
        
        // Extract the Euler angles
        Vector3 eulerRotation = lookRotation.eulerAngles;
        
        // Only keep the rotations for axes we want to rotate
        Vector3 currentRotation = transform.rotation.eulerAngles;
        
        // Selectively apply rotations based on which axes we want to rotate
        if (!rotateX) eulerRotation.x = _originalXRotation;
        if (!rotateY) eulerRotation.y = currentRotation.y;
        if (!rotateZ) eulerRotation.z = _originalZRotation;
        
        // Apply any rotation offset
        eulerRotation += rotationOffset;
        
        // Convert back to quaternion
        Quaternion targetRotation = Quaternion.Euler(eulerRotation);
        
        // Apply the rotation (with or without smoothing)
        if (smoothRotation)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            transform.rotation = targetRotation;
        }
        
        // Apply distance-based scaling if enabled
        if (scaleWithDistance)
        {
            // Calculate scale factor based on distance (clamped between min and max distance)
            float normalizedDistance = Mathf.Clamp(distanceToCamera, minDistance, maxDistance);
            float scaleFactor = normalizedDistance / minDistance;
            
            // Calculate target scale
            Vector3 targetScale = _originalScale * scaleFactor;
            
            // Apply scaling with or without smoothing
            if (smoothScaling)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scalingSpeed * Time.deltaTime);
            }
            else
            {
                transform.localScale = targetScale;
            }
        }
    }
}