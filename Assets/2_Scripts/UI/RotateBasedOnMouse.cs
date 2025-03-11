using System;
using UnityEngine;
using UnityEngine.Serialization;

public class RotateBasedOnMouse : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float rotationSensitivity = 15f; // Maximum rotation in degrees
    [SerializeField] private float smoothSpeed = 5f; // How quickly the rotation adjusts
    [SerializeField] private float maxXRotation = 30f; // Maximum X-axis rotation
    [SerializeField] private float maxYRotation = 45f; // Maximum Y-axis rotation
    
    private Quaternion _initialRotation;
    private Quaternion _targetRotation;
    private bool _enableEffect;
    
    private void Awake()
    {
        _initialRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        _enableEffect = true;
    }
    
    private void OnDisable()
    {
        _enableEffect = false;
    }

    private void LateUpdate()
    {
        if (!_enableEffect) return;
        
        // Get mouse position in screen space (0 to 1)
        float mouseX = Input.mousePosition.x / Screen.width;
        float mouseY = Input.mousePosition.y / Screen.height;
        
        // Convert to -1 to 1 range
        mouseX = (mouseX * 2f) - 1f;
        mouseY = (mouseY * 2f) - 1f;
        
        // Calculate rotation based on mouse position
        float rotX = -mouseY * rotationSensitivity; // Invert Y for natural feeling
        float rotY = mouseX * rotationSensitivity;
        
        // Clamp rotations to maximum values
        rotX = Mathf.Clamp(rotX, -maxXRotation, maxXRotation);
        rotY = Mathf.Clamp(rotY, -maxYRotation, maxYRotation);
        
        // Create target rotation
        _targetRotation = _initialRotation * Quaternion.Euler(rotX, rotY, 0);
        
        // Smoothly rotate towards target rotation
        transform.localRotation = Quaternion.Slerp(transform.localRotation, _targetRotation, smoothSpeed * Time.deltaTime);
    }
}