
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

public class PlayerTeleportingState : PlayerBaseState
{
    public PlayerTeleportingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    private bool _teleportationComplete = false;
    private float _teleportationTime = 2f;
    private float _teleportationTimer = 0f;
    private Vector3 _teleportationDestination = new Vector3(2,2,2);
    
    public override void EnterState()
    {
        StateMachine.ResetGravity();
        StateMachine.DisableAiming();
        if (TestManager.Instance)
        {
            _teleportationDestination = TestManager.Instance.GetCheckpointPosition();
        }
    }
    
    public override void ExitState()
    {
        _teleportationComplete = false;
        _teleportationTimer = 0f;
    }

    public override void UpdateState()
    {
        if (!_teleportationComplete && _teleportationTimer < _teleportationTime)
        {
            _teleportationTimer += Time.deltaTime;
            if (_teleportationTimer >= _teleportationTime)
            {
                _teleportationComplete = true;
            }
        }
        
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.HandleMovement(new PlayerStateMachine.MovementParams(
            isAirborne: false,
            speedMultiplier: 0f,
            accelMultiplier: 3f,  
            controlMultiplier: 1.0f
        ));
        
        StateMachine.HandleRotation(new PlayerStateMachine.RotationParams(
            useAimRotation: false,
            rotationMultiplier: 0f,
            allowRotation: false
        ));
    }
    
    private void CheckStateTransitions()
    {
        if (_teleportationComplete)
        {
            StateMachine.Teleport(_teleportationDestination, quaternion.identity);
            StateMachine.SwitchState(StateMachine.GroundedState);
        }
    }
    
}