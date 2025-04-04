using System;
using TMPro;
using UnityEngine;
using VInspector;

public class TutorialText : MonoBehaviour
{
    [Header("Rotate Towards")]
    [SerializeField] private bool rotateX = true;
    [SerializeField] private bool rotateY = true;
    [SerializeField] private bool rotateZ = false;
    [ShowIf("ShouldRotate")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    [SerializeField] private Transform rotationTarget;
    [SerializeField,ReadOnly] private float distanceToRotationTarget;
    [EndIf]
    
    
    [Header("Distance Scaling")]
    [SerializeField] private bool scaleWithDistance = false;
    [ShowIf("scaleWithDistance")]
    [SerializeField] private float scalingSpeed = 10f;
    [SerializeField] private float minDistance = 7f;
    [SerializeField] private float maxDistance = 18f;
    [SerializeField] private Transform distanceTarget;
    [SerializeField,ReadOnly] private float distanceToDistanceTarget;
    [EndIf]
    
    [Header("Distance Fade")]
    [SerializeField] private bool fadeWithDistance = true;
    [ShowIf("fadeWithDistance")]
    [SerializeField] private float fadingSpeed = 8f;
    [SerializeField] private float minFadeDistance = 5f;
    [SerializeField] private float maxFadeDistance = 15f;
    [SerializeField] private Transform fadeTarget;
    [SerializeField,ReadOnly] private float distanceToFadeTarget;
    [EndIf]
    
    [Header("Distance Positioner")]
    [SerializeField] private bool positionWithDistance = true;
    [ShowIf("positionWithDistance")]
    [SerializeField] private float positioningSpeed = 8f;
    [SerializeField] private float minPositionDistance = 5f;
    [SerializeField] private float maxPositionDistance = 15f;
    [SerializeField] private Vector3 farthestPosition = Vector3.down;
    [SerializeField] private Transform positionTarget;
    [SerializeField,ReadOnly] private float distanceToPositionerTarget;
    [EndIf]
    
    private Vector3 _originalScale;
    private Vector3 _originalPosition;
    private float _originalXRotation;
    private float _originalZRotation;
    private float _originalYRotation;
    private TextMeshProUGUI _text;
    private CanvasGroup _canvasGroup;
    private PlayerStateMachine _player;
    private bool ShouldRotate => rotateX || rotateY || rotateZ;


    private void OnValidate()
    {
        if (Application.isPlaying) return;
        
        distanceToPositionerTarget = 0f;
        distanceToFadeTarget = 0f;
        distanceToDistanceTarget = 0f;
        distanceToRotationTarget = 0f;
    }

    private void Awake()
    {
        _text = GetComponentInChildren<TextMeshProUGUI>();
        _canvasGroup = GetComponent<CanvasGroup>();
        
        if (!_canvasGroup && fadeWithDistance)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        Vector3 currentRotation = transform.rotation.eulerAngles;
        _originalXRotation = currentRotation.x;
        _originalZRotation = currentRotation.z;
        _originalYRotation = currentRotation.y;
        _originalScale = transform.localScale;
        _originalPosition = transform.localPosition;
    }

    private void Start()
    {
        
        _player = PlayerStateMachine.Instance;
        
        if (!rotationTarget && Camera.main) rotationTarget = Camera.main.transform;
        if (!distanceTarget&& _player) distanceTarget = _player.transform;
        if (!fadeTarget && _player) fadeTarget = _player.transform;
        if (!positionTarget && _player) positionTarget = _player.transform;
    }
    
    private void Update()
    {
        RotateTowardsCamera();
        ScaleBasedOnDistance();
        FadeBasedOnDistance();
        PositionBasedOnDistance();
    }

    private void RotateTowardsCamera()
    {
        if (!rotationTarget || !ShouldRotate) return;
        
        // Get the direction from the object to the camera
        Vector3 directionToTarget = rotationTarget.position - transform.position;
        
        // Calculate distance to target
        distanceToRotationTarget = directionToTarget.magnitude;
        
        // Create a rotation that looks at the camera
        Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
        
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
        
        // Apply the rotation with smoothing
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void ScaleBasedOnDistance()
    {
        if (!distanceTarget || !scaleWithDistance) return;
        
        // Get the direction from the object to the camera
        Vector3 directionToTarget = distanceTarget.position - transform.position;
        
        // Calculate distance to camera for scaling
        distanceToDistanceTarget = directionToTarget.magnitude;
        
        // Calculate scale factor based on distance (clamped between min and max distance)
        float normalizedDistance = Mathf.Clamp(distanceToDistanceTarget, minDistance, maxDistance);
        float scaleFactor = normalizedDistance / minDistance;
            
        // Calculate target scale
        Vector3 targetScale = _originalScale * scaleFactor;
            
        // Apply scaling with smoothing
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scalingSpeed * Time.deltaTime);
    }
    
    private void FadeBasedOnDistance()
    {
        if (!fadeTarget || !fadeWithDistance || !_canvasGroup) return;
        
        // Get the direction from the object to the target
        Vector3 directionToTarget = fadeTarget.position - transform.position;
        
        // Calculate distance to target
        distanceToFadeTarget = directionToTarget.magnitude;
        
        // Calculate alpha based on distance (inverse relationship: closer = more visible)
        float normalizedDistance = Mathf.Clamp(distanceToFadeTarget, minFadeDistance, maxFadeDistance);
        float range = maxFadeDistance - minFadeDistance;
        
        // When distance is at minFadeDistance, alpha should be 1
        // When distance is at maxFadeDistance, alpha should be 0
        float targetAlpha = range > 0 ? 1 - ((normalizedDistance - minFadeDistance) / range) : 1;
        
        // Apply fading with smoothing
        _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, targetAlpha, fadingSpeed * Time.deltaTime);
    }
    
    private void PositionBasedOnDistance()
    {
        if (!positionTarget || !positionWithDistance) return;
        
        // Get the direction from the object to the target
        Vector3 directionToTarget = positionTarget.position - transform.position;
        
        // Calculate distance to target
        distanceToPositionerTarget = directionToTarget.magnitude;
        
        // Calculate position factor based on distance
        float normalizedDistance = Mathf.Clamp(distanceToPositionerTarget, minPositionDistance, maxPositionDistance);
        float range = maxPositionDistance - minPositionDistance;
        
        // When distance is at minPositionDistance, we should be at original position
        // When distance is at maxPositionDistance, we should be at farthestPosition
        float positionFactor = range > 0 ? (normalizedDistance - minPositionDistance) / range : 0;
        
        // Calculate target position
        Vector3 targetPosition = Vector3.Lerp(_originalPosition, _originalPosition + farthestPosition, positionFactor);
        
        // Apply positioning with smoothing
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, positioningSpeed * Time.deltaTime);
    }
}