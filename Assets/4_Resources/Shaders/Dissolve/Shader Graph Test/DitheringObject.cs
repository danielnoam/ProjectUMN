using System;
using UnityEngine;

public class DitheringObject : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private SubscribeTo subscribeTo = SubscribeTo.Player;
    
    private enum SubscribeTo { None, Player, Robot, RobotLight, }
    private bool SubscribeToNone => subscribeTo == SubscribeTo.None;
    private Transform _target;
    private static readonly int PositionID = Shader.PropertyToID("_Dither_Object_Position");

    
    private void Start()
    {
        if (SubscribeToNone) return;

        switch (subscribeTo)
        {
            case SubscribeTo.Robot:
                if (!TestManager.Instance || !TestManager.Instance.Robot) return;
                _target = TestManager.Instance.Robot.transform;
                break;
            case SubscribeTo.RobotLight:
                if (!TestManager.Instance || !TestManager.Instance.Robot) return;
                
                _target = TestManager.Instance.Robot.LightDissolver.transform;;
                break;
            case SubscribeTo.Player:
                
                if (!PlayerStateMachine.Instance) return;
                
                _target = PlayerStateMachine.Instance.transform;
                break;
        }
    }
    
    private void OnEnable()
    {
        if (SubscribeToNone) return;
        
        switch (subscribeTo)
        {
            case SubscribeTo.Robot:
                if (!TestManager.Instance || !TestManager.Instance.Robot) return;
                _target = TestManager.Instance.Robot.transform;
                break;
            case SubscribeTo.RobotLight:
                if (!TestManager.Instance || !TestManager.Instance.Robot) return;
                
                _target = TestManager.Instance.Robot.LightDissolver.transform;;
                break;
            case SubscribeTo.Player:
                
                if (!PlayerStateMachine.Instance) return;
                
                _target = PlayerStateMachine.Instance.transform;
                break;
        }
    }
    
    private void OnDisable()
    {
        _target = null;
    }


    private void Update()
    {
        UpdateTargetPosition();
    }
    
    
    private void UpdateTargetPosition()
    {
        if (!_target) return;
        
        Shader.SetGlobalVector(PositionID, _target.position);
    }
}
