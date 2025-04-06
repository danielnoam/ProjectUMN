using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using VInspector;

public class EventTriggerObject : MonoBehaviour
{

    [Header("Settings")] 
    [SerializeField] private TriggerShape triggerShape = TriggerShape.Sphere;
    [SerializeField] private LayerMask triggerLayers;
    [SerializeField] private bool continuesCollision;
    [SerializeField] private bool triggerOnce;
    [SerializeField] private bool commandRobotOnTrigger;
    [SerializeField, ShowIf("commandRobotOnTrigger")] private CommandToSend commandToSend = CommandToSend.Follow; [EndIf]
    [SerializeField] private UnityEvent events;
    
    
    
    private enum TriggerShape { Box, Sphere }
    private bool _triggered;
    private BoxCollider _boxCollider;
    private SphereCollider _sphereCollider;




    private void OnTriggerEnter(Collider other)
    {
        
        if ((triggerLayers.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }
        
        if (_triggered && triggerOnce) return;
        
        if (other.TryGetComponent(out PlayerStateMachine player))
        {
            if (commandRobotOnTrigger)
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
                    }
                }
            }
            events?.Invoke();
            _triggered = true;
            if (!triggerOnce) StartCoroutine(RestartState());
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (!continuesCollision) return;
        if ((triggerLayers.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }
        if (_triggered && triggerOnce) return;

        if (other.TryGetComponent(out PlayerStateMachine player))
        {
            if (commandRobotOnTrigger)
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
                    }
                }
            }
            events?.Invoke();
            _triggered = true;
            if (!triggerOnce) StartCoroutine(RestartState());
        }
    }


    private IEnumerator RestartState()
    {
        yield return  new WaitForSeconds(0.5f);
        _triggered = false;
    }

    
    
    private void OnValidate()
    {
        if (!commandRobotOnTrigger)
        {
            commandToSend = CommandToSend.Nothing;
        }
        
        _boxCollider = GetComponent<BoxCollider>();
        if (!_boxCollider)
        {
            _boxCollider = gameObject.AddComponent<BoxCollider>();
        }
        _sphereCollider = GetComponent<SphereCollider>();
        if (!_sphereCollider)
        {
            _sphereCollider = gameObject.AddComponent<SphereCollider>();
        }
        
        
        if (triggerShape == TriggerShape.Sphere)
        {

            _sphereCollider.isTrigger = true;
            _sphereCollider.enabled = true;
            _boxCollider.enabled = false;
            _boxCollider.size = Vector3.zero;
            
        }
        else if (triggerShape == TriggerShape.Box)
        {

            _boxCollider.isTrigger = true;
            _boxCollider.enabled = true;
            _sphereCollider.enabled = false;
            _sphereCollider.radius = 0f;
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

        if (commandRobotOnTrigger)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.green;
            style.fontSize = 8;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            UnityEditor.Handles.Label(transform.position + new Vector3(0f, 0.5f, 0f), "Command Robot " + commandToSend, style);
        }

    }
#endif
}
