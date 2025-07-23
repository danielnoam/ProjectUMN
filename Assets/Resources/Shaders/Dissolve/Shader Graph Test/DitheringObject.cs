using System;
using UnityEngine;
using VInspector;

public class DitheringObject : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private SubscribeTo subscribeTo = SubscribeTo.Player;
    [SerializeField,ReadOnly] private Transform _target;
    private enum SubscribeTo { None, Player, Robot, RobotLight, }
    private bool SubscribeToNone => subscribeTo == SubscribeTo.None;
    private static readonly int PositionID = Shader.PropertyToID("_Dither_Object_Position");
    private Material _material;

    private void Awake()
    {
        Renderer randerer = GetComponent<Renderer>();
        _material = randerer.material;
        randerer.material = new Material(_material);
        _material = randerer.material;
    }

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
                
                _target = TestManager.Instance.Robot.LightDissolver.transform;
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
        _material.SetVector(PositionID, _target.position);
    }
}
