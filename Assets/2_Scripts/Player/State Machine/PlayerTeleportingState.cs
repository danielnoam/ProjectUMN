
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

public class PlayerTeleportingState : PlayerBaseState
{
    
    public PlayerTeleportingState(PlayerStateMachine stateMachine, Vector3 destination, Quaternion rotation, float teleportationTime) : base(stateMachine)
    {
        StateMachine.TeleportingState = this;
        _teleportationDestination = destination;
        _teleportationRotation = rotation;
        _teleportationTime = teleportationTime;
    }

    private readonly float _teleportationTime;
    private readonly Vector3 _teleportationDestination;
    private readonly Quaternion _teleportationRotation;
    private bool _teleportationComplete = false;
    private float _teleportationTimer = 0f;

    
    public override void EnterState()
    {
        StateMachine.SetCharacterCollider(false);
        StateMachine.ResetGravity();
        StateMachine.ClearCurrentAimedInteractable();
        StateMachine.ClearCurrentInteractable();
        StateMachine.transform.position = _teleportationDestination;
        StateMachine.transform.rotation = _teleportationRotation;
    }
    
    public override void ExitState()
    {
        _teleportationComplete = false;
        _teleportationTimer = 0f;
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
            StateMachine.onPlayerSpawned?.Invoke();
            StateMachine.SwitchState(StateMachine.GroundedState);
        }
    }
    
    
}