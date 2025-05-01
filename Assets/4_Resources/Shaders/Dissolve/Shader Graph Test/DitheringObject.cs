using System;
using UnityEngine;

public class DitheringObject : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool subscribeToPlayer = true;
    
    private Transform _target;
    private static readonly int PositionID = Shader.PropertyToID("_Dither_Object_Position");

    
    private void Start()
    {
        if (!PlayerStateMachine.Instance || !subscribeToPlayer) return;
        
        _target = PlayerStateMachine.Instance.transform;
    }
    
    private void OnEnable()
    {
        if (!PlayerStateMachine.Instance || !subscribeToPlayer) return;
        
        _target = PlayerStateMachine.Instance.transform;
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
