using UnityEngine;

public class IdleStand : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform idlePosition;
    
    
    
    
    public Transform IdlePosition => idlePosition;
}
