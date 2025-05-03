using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using VInspector;

public class EventTriggerObject : MonoBehaviour
{

    [Header("Settings")] 
    [SerializeField] private TriggerShape triggerShape = TriggerShape.Sphere;
    [SerializeField] private TriggeredBy triggeredBy = TriggeredBy.Player;
    [SerializeField] private LayerMask triggerLayers;
    [SerializeField] private bool triggerOnce;
    [SerializeField] private bool allowRobotTeleportInTrigger = true;
    [SerializeField] private bool commandRobotOnTrigger;
    [SerializeField, ShowIf("commandRobotOnTrigger")] private CommandToSend commandToSend = CommandToSend.Follow; [EndIf]
    
    
    [SerializeField] private UnityEvent onEnterEvent;
    [SerializeField] private UnityEvent onStayEvent;
    
    
    
    private enum TriggerShape { Box, Sphere }
    private enum TriggeredBy { Player, Robot, Both }
    private bool _triggered;
    private BoxCollider _boxCollider;
    private SphereCollider _sphereCollider;
    private bool TriggeredByPlayer => triggeredBy is TriggeredBy.Player or TriggeredBy.Both;
    private bool TriggeredByRobot => triggeredBy is TriggeredBy.Robot or TriggeredBy.Both;

    

    private void OnTriggerEnter(Collider other)
    {
        
        if ((triggerLayers.value & (1 << other.gameObject.layer)) == 0) return;
        
        if (_triggered && triggerOnce) return;
        
        
        if (TriggeredByPlayer && other.TryGetComponent(out PlayerStateMachine player))
        {

            if (commandRobotOnTrigger) SendRobotCommand();
            if (!allowRobotTeleportInTrigger) UpdateRobotTeleportState(false);
            onEnterEvent?.Invoke();
            _triggered = true;
            if (!triggerOnce) StartCoroutine(RestartState());
        }
        
        if (TriggeredByRobot && other.TryGetComponent(out RobotCompanion robot))
        {
            if (!allowRobotTeleportInTrigger) UpdateRobotTeleportState(false);
            onEnterEvent?.Invoke();
            _triggered = true;
            if (!triggerOnce) StartCoroutine(RestartState());
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if ((triggerLayers.value & (1 << other.gameObject.layer)) == 0) return;

        if (TriggeredByPlayer && other.TryGetComponent(out PlayerStateMachine player))
        {
            onStayEvent?.Invoke();
        }
        
        if (TriggeredByRobot && other.TryGetComponent(out RobotCompanion robot))
        {
            onStayEvent?.Invoke();
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if ((triggerLayers.value & (1 << other.gameObject.layer)) == 0) return;
        
        if (TriggeredByPlayer && other.TryGetComponent(out PlayerStateMachine player))
        {
            UpdateRobotTeleportState(true);
        }
        
        if (TriggeredByRobot && other.TryGetComponent(out RobotCompanion robot))
        {
            UpdateRobotTeleportState(true);
        }
    }
    
    
    

    private IEnumerator RestartState()
    {
        yield return  new WaitForSeconds(0.5f);
        _triggered = false;
    }

    private void SendRobotCommand()
    {
        if (TestManager.Instance && TestManager.Instance.Robot)
        {
            switch (commandToSend)
            {
                case CommandToSend.Idle:
                    TestManager.Instance.Robot.CommandIdle();
                    break;
                case CommandToSend.Follow:
                    TestManager.Instance.Robot.CommandFollowPlayer();
                    break;
                case CommandToSend.Sit:
                    TestManager.Instance.Robot.CommandSitDown();
                    break;
                case CommandToSend.Nothing:
                    break;
                
                default:
                    Debug.Log($"Command {commandToSend} not recognized. No action taken.");
                    break;
            }
        }
    }

    private void UpdateRobotTeleportState(bool state)
    {
        if (TestManager.Instance && TestManager.Instance.Robot)
        {
            TestManager.Instance?.Robot?.SetStuckTeleportState(state);
        }
    }

    
    private void OnValidate()
{
    if (!commandRobotOnTrigger)
    {
        commandToSend = CommandToSend.Nothing;
    }
    
    // Only get or add components if needed, not every time
    if (triggerShape == TriggerShape.Box)
    {
        // Ensure box collider exists
        _boxCollider = GetComponent<BoxCollider>();
        if (!_boxCollider)
        {
            _boxCollider = gameObject.AddComponent<BoxCollider>();
        }
        
        // Configure box collider
        _boxCollider.isTrigger = true;
        _boxCollider.enabled = true;
        
        // Handle sphere collider if it exists
        _sphereCollider = GetComponent<SphereCollider>();
        if (_sphereCollider)
        {
            _sphereCollider.enabled = false;
        }
    }
    else if (triggerShape == TriggerShape.Sphere)
    {
        // Ensure sphere collider exists
        _sphereCollider = GetComponent<SphereCollider>();
        if (!_sphereCollider)
        {
            _sphereCollider = gameObject.AddComponent<SphereCollider>();
        }
        
        // Configure sphere collider
        _sphereCollider.isTrigger = true;
        _sphereCollider.enabled = true;
        
        // Handle box collider if it exists
        _boxCollider = GetComponent<BoxCollider>();
        if (_boxCollider)
        {
            _boxCollider.enabled = false;
        }
    }
    
    // Check for any conflicting colliders that might interfere
    Collider[] existingColliders = GetComponents<Collider>();
    foreach (Collider collider in existingColliders)
    {
        // Skip our managed colliders
        if (collider == _boxCollider || collider == _sphereCollider) continue;
        
        // Log a warning about potential conflicts
        if (!collider.isTrigger)
        {
            Debug.LogWarning($"EventTriggerObject on {gameObject.name} found another non-trigger collider " +
                             $"({collider.GetType().Name}) that might conflict with trigger detection.", this);
        }
    }
}

#if UNITY_EDITOR
    
    
    private void OnDrawGizmos()
    {
        if (triggerShape == TriggerShape.Box)
        {
            if (!_boxCollider) return;
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, _boxCollider.bounds.size);
        }
        else if (triggerShape == TriggerShape.Sphere)
        {
            if (!_sphereCollider) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _sphereCollider.radius);
        }

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.green;
        style.fontSize = 8;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        
        if (commandRobotOnTrigger)
        {
            UnityEditor.Handles.Label(transform.position + new Vector3(0f, 0.5f, 0f), "Command Robot " + commandToSend, style);
        }
            
        if (!allowRobotTeleportInTrigger)
        {
            UnityEditor.Handles.Label(transform.position + new Vector3(0f, 0.7f, 0f), "Prevent Robot Teleport", style);
        }
        
        if (onEnterEvent.GetPersistentEventCount() > 0)
        {
            UnityEditor.Handles.Label(transform.position + new Vector3(0f, 0.9f, 0f), "On Enter Event", style);
        }
        
        if (onStayEvent.GetPersistentEventCount() > 0)
        {
            UnityEditor.Handles.Label(transform.position + new Vector3(0f, 1.1f, 0f), "On Stay Event", style);
        }


    }
#endif
}
