using System;
using UnityEngine;
using UnityEngine.Serialization;
using VInspector;

public class PlayerCircularWorldWrap : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool isWrapEnabled;
    [SerializeField] private float defaultWorldRadius = 50f;
    [SerializeField] private Vector3 defaultCenterPosition = Vector3.zero;
    

    [Header("References")]
    [SerializeField] private PlayerStateMachine player;
    

    [SerializeField, ReadOnly] private float _worldRadius;
    [SerializeField, ReadOnly] private Vector3 _centerPosition;
    
    private void Start()
    {
        player?.TestManager?.onTestStartLoading.AddListener(UpdateWorldSettings);
        player?.TestManager?.onTestStartUnloading.AddListener(DisableWarping);
    }
    
    private void OnEnable()
    {
        player?.TestManager?.onTestStartLoading.AddListener(UpdateWorldSettings);
        player?.TestManager?.onTestStartUnloading.AddListener(DisableWarping);
    }
    
    private void OnDisable()
    {
        player?.TestManager?.onTestStartLoading.RemoveListener(UpdateWorldSettings);
        player?.TestManager?.onTestStartUnloading.RemoveListener(DisableWarping);
    }

    private void Update()
    {
        // Skip if wrapping is disabled
        if (!isWrapEnabled)
            return;
            
        Vector3 position = transform.position;
        position.y = transform.position.y; // Keep original Y value
        
        // Calculate distance from center in the XZ plane, using _centerPosition
        Vector2 centerXZ = new Vector2(_centerPosition.x, _centerPosition.z);
        Vector2 positionXZ = new Vector2(position.x, position.z);
        
        // Get direction and distance relative to center position
        Vector2 directionFromCenter = positionXZ - centerXZ;
        float distanceFromCenter = directionFromCenter.magnitude;
        
        // Check if outside the circular boundary
        if (distanceFromCenter > _worldRadius)
        {
            // Calculate normalized direction from center
            Vector2 normalizedDirection = directionFromCenter.normalized;
            
            // Calculate new position on the opposite side
            Vector2 newPositionXZ = centerXZ - normalizedDirection * _worldRadius;
            
            // Update position
            position.x = newPositionXZ.x;
            position.z = newPositionXZ.y;
            
            // Apply the new position
            transform.position = position;
        }
    }
    
    private void UpdateWorldSettings(SOTest test)
    {
        
        if (test && test.WorldEdges)
        {
            isWrapEnabled = true;
            _worldRadius = test.WorldRadius;
            _centerPosition = test.WorldCenterPosition;
        }
        else if (test && !test.WorldEdges)
        {
            DisableWarping(test);
        }
        else
        {
            isWrapEnabled = true;
            SetWorldSettingsDefault();
        }

    }
    
    private void SetWorldSettingsDefault()
    {
        _worldRadius = defaultWorldRadius;
        _centerPosition = defaultCenterPosition;
    }

    private void DisableWarping(SOTest test)
    {
        isWrapEnabled = false;
        SetWorldSettingsDefault();
    }


    
    private void OnDrawGizmosSelected()
    {
        if (!isWrapEnabled) return;
        
        // Use either the set radius or default for gizmos
        float gizmosRadius = _worldRadius > 0 ? _worldRadius : defaultWorldRadius;
        Vector3 gizmosCenter = _centerPosition != Vector3.zero ? _centerPosition : defaultCenterPosition;
        
        // Draw the circular boundary
        Gizmos.color = Color.red;
        
        // Draw the main circle at Y=0
        DrawCircle(gizmosCenter, gizmosRadius, 64);
        
        // Mark the current player position
        Gizmos.DrawSphere(transform.position, 0.5f);
        
        // Draw a line from center to the player
        Gizmos.DrawLine(new Vector3(gizmosCenter.x, transform.position.y, gizmosCenter.z), transform.position);
        
        // Draw the opposite point where player would teleport to
        Vector3 pos = transform.position;
        Vector2 centerXZ = new Vector2(gizmosCenter.x, gizmosCenter.z);
        Vector2 posXZ = new Vector2(pos.x, pos.z);
        Vector2 dirFromCenter = posXZ - centerXZ;
        float distance = dirFromCenter.magnitude;
        
        if (distance > 0)
        {
            Vector2 dir = dirFromCenter.normalized;
            Vector2 oppositeXZ = centerXZ - dir * gizmosRadius;
            Vector3 oppositePoint = new Vector3(oppositeXZ.x, pos.y, oppositeXZ.y);
            
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(oppositePoint, 0.5f);
            Gizmos.DrawLine(new Vector3(gizmosCenter.x, pos.y, gizmosCenter.z), oppositePoint);
        }
        
        // If wrap is disabled, show a visual indication
        if (!isWrapEnabled)
        {
            Gizmos.color = Color.yellow;
            Vector3 centerPos = new Vector3(gizmosCenter.x, 0, gizmosCenter.z);
            Gizmos.DrawLine(centerPos - Vector3.right * 2, centerPos + Vector3.right * 2);
            Gizmos.DrawLine(centerPos - Vector3.forward * 2, centerPos + Vector3.forward * 2);
        }
    }
    
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 2 * Mathf.PI / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep;
            float angle2 = (i + 1) * angleStep;
            
            Vector3 point1 = new Vector3(
                center.x + radius * Mathf.Cos(angle1),
                center.y,
                center.z + radius * Mathf.Sin(angle1)
            );
            
            Vector3 point2 = new Vector3(
                center.x + radius * Mathf.Cos(angle2),
                center.y,
                center.z + radius * Mathf.Sin(angle2)
            );
            
            Gizmos.DrawLine(point1, point2);
        }
    }
}