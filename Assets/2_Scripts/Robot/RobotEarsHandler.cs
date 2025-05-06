using System;
using UnityEngine;

public class RobotEarsHandler : MonoBehaviour
{
    
    [Header("Ears rotation")] 
    [SerializeField] private bool rotateEars = true;

    [Tooltip("Maximum bend angle of the ears")]
    [SerializeField] private float maxEarBend = 45f;

    [Tooltip("How smoothly the ears rotate")]
    [SerializeField] private float earRotationSmoothness = 0.2f;

    [Tooltip("Speed at which ears reach their maximum bend")]
    [SerializeField] private float maxSpeedForEarRotation = 4f;
    
    
    [Header("References")] 
    [SerializeField] private RobotCompanion robot;
    [SerializeField] private Transform leftEarPivot;
    [SerializeField] private Transform rightEarPivot;
    [SerializeField] private Rigidbody rigidBody;
    private Quaternion _leftEarBaseRotation;
    private Quaternion _rightEarBaseRotation;


    private void Awake()
    {
        if (leftEarPivot) _leftEarBaseRotation = leftEarPivot.localRotation;
        if (rightEarPivot) _rightEarBaseRotation = rightEarPivot.localRotation;
    }

    private void Update()
    {
        UpdateEarRotation();
    }

    private void UpdateEarRotation() 
   {
        if (!rotateEars || !leftEarPivot || !rightEarPivot) return;

        // Get the movement direction in local space
        Vector3 localVelocity = transform.InverseTransformDirection(rigidBody.linearVelocity);
        Vector3 localAngularVelocity = transform.InverseTransformDirection(rigidBody.angularVelocity);

        float movementSpeed = rigidBody.linearVelocity.magnitude;
        float rotationSpeed = rigidBody.angularVelocity.magnitude;

        // Calculate bend strength for both movement and rotation
        float movementBendStrength = Mathf.Clamp01(movementSpeed / maxSpeedForEarRotation);
        float rotationBendStrength = Mathf.Clamp01(rotationSpeed / robot.MaxAngularVelocity);

        // Calculate movement-based rotation
        Vector3 movementRotation = new Vector3(
            -localVelocity.z, // Forward/back movement causes up/down rotation
            -localVelocity.x, // Left/right movement causes side rotation
            0
        ).normalized * (maxEarBend * movementBendStrength);

        // Calculate rotation-based ear bend
        // For the left ear
        Vector3 leftRotationBend = new Vector3(
            0,
            localAngularVelocity.y, // Yaw rotation causes side bend
            0
        ) * (maxEarBend * rotationBendStrength);

        // For the right ear (opposite of left ear for rotation)
        Vector3 rightRotationBend = new Vector3(
            0,
            localAngularVelocity.y, // Opposite direction for right ear
            0
        ) * (maxEarBend * rotationBendStrength);

        // Combine movement and rotation effects
        Quaternion leftTargetRotation = Quaternion.Euler(movementRotation + leftRotationBend);
        Quaternion rightTargetRotation = Quaternion.Euler(movementRotation + rightRotationBend);

        // Apply rotation with smoothing
        leftEarPivot.localRotation = Quaternion.Slerp(
            leftEarPivot.localRotation,
            leftTargetRotation * _leftEarBaseRotation,
            1f - Mathf.Pow(earRotationSmoothness, Time.deltaTime)
        );

        rightEarPivot.localRotation = Quaternion.Slerp(
            rightEarPivot.localRotation,
            rightTargetRotation * _rightEarBaseRotation,
            1f - Mathf.Pow(earRotationSmoothness, Time.deltaTime)
        );
   }
}
