
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

public class PlayerTeleportingState : PlayerBaseState
{
    
    public PlayerTeleportingState(PlayerStateMachine stateMachine, Vector3 destination, Quaternion rotation, float teleportationTime, bool fromCheckpoint) : base(stateMachine)
    {
        StateMachine.TeleportingState = this;
        _teleportationDestination = destination;
        _teleportationRotation = rotation;
        _teleportationTime = teleportationTime;
        _fromCheckpoint = fromCheckpoint;
    }

    private readonly float _teleportationTime;
    private readonly Vector3 _teleportationDestination;
    private readonly Quaternion _teleportationRotation;
    private bool _teleportationComplete = false;
    private bool _fromCheckpoint = false;
    private float _teleportationTimer = 0f;

    
    public override void EnterState()
    {
        StateMachine.SetCharacterCollider(false);
        StateMachine.ResetGravity();
        StateMachine.ResetVelocity();   
        StateMachine.ClearCurrentAimedInteractable();
        StateMachine.ClearCurrentInteractable();
        StateMachine.transform.position = _teleportationDestination;
        StateMachine.transform.rotation = _teleportationRotation;
    }
    
    public override void ExitState()
    {
        if (_fromCheckpoint)
        {
            StateMachine.onPlayerSpawnedFromCheckpoint?.Invoke();
        }
        else
        {
            StateMachine.onPlayerSpawned?.Invoke();
        }
        _fromCheckpoint = false;
        _teleportationComplete = false;
        _teleportationTimer = 0f;
        StateMachine.ResetVelocity();   
        StateMachine.SetCharacterCollider(true);
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
        
        StateMachine.HandleAiming(false);
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.HandleMovement(allowMovement: false, isAirborne: false);
        StateMachine.HandleRotation(allowRotation: false, alignWithCameraWhenIdle: false);
    }
    
    private void CheckStateTransitions()
    {
        if (_teleportationComplete)
        {
            StateMachine.SwitchState(StateMachine.GroundedState);
        }
    }
    
    
}