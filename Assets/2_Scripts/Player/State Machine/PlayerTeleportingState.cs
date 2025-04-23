
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

public enum TeleportationType
{
    Checkpoint,
    SpawnPoint,
    EndPoint
}

public class PlayerTeleportingState : PlayerBaseState
{
    
    public PlayerTeleportingState(PlayerStateMachine stateMachine, Vector3 destination, Quaternion rotation, float time, TeleportationType type) : base(stateMachine)
    {
        StateMachine.TeleportingState = this;
        _teleportationDestination = destination;
        _teleportationRotation = rotation;
        _time = time;
        _teleportationType = type;
    }

    private readonly float _time;
    private readonly Vector3 _teleportationDestination;
    private readonly Quaternion _teleportationRotation;
    private bool _teleportationComplete = false;
    private float _teleportationTimer = 0f;
    private TeleportationType _teleportationType;

    
    public override void EnterState()
    {
        
        StateMachine.ResetVerticalVelocity();
        StateMachine.ResetHorizontalVelocity();   
        StateMachine.ClearCurrentAimedInteractable();
        StateMachine.ClearCurrentInteractable();
        switch (_teleportationType)
        {
            case TeleportationType.Checkpoint:
                StateMachine.SetCharacterColliderState(false);
                StateMachine.SetCharacterPosition(_teleportationDestination, _teleportationRotation);
                break;
            case TeleportationType.SpawnPoint:
                StateMachine.SetCharacterColliderState(false);
                StateMachine.SetCharacterPosition(_teleportationDestination, _teleportationRotation);
                break;
            case TeleportationType.EndPoint:
                break;
        }
        
    }
    
    public override void ExitState()
    {
        switch (_teleportationType)
        {
            case TeleportationType.Checkpoint:
                StateMachine.onPlayerSpawnedFromCheckpoint?.Invoke();
                StateMachine.SetCharacterColliderState(true);
                break;
            case TeleportationType.SpawnPoint:
                StateMachine.onPlayerSpawned?.Invoke();
                StateMachine.SetCharacterColliderState(true);
                break;
            case TeleportationType.EndPoint:
                break;
        }
        
        _teleportationComplete = false;
        _teleportationTimer = 0f;
        StateMachine.ResetHorizontalVelocity();   
    }

    public override void UpdateState()
    {
        switch (_teleportationType)
        {
            case TeleportationType.Checkpoint:
                CheckStateTransitions();
                break;
            case TeleportationType.SpawnPoint:
                CheckStateTransitions();
                break;
            case TeleportationType.EndPoint:
                
                break;
        }
        
        if (!_teleportationComplete && _teleportationTimer < _time)
        {
            _teleportationTimer += Time.deltaTime;
            if (_teleportationTimer >= _time)
            {
                _teleportationComplete = true;
            }
        }
        
        StateMachine.HandleAiming(false);
    }

    public override void FixedUpdateState()
    {
        switch (_teleportationType)
        {
            case TeleportationType.Checkpoint:
                StateMachine.HandleMovement(allowMovement: false, isAirborne: false);
                StateMachine.HandleRotation(allowRotation: false, alignWithCameraWhenIdle: false);
                break;
            case TeleportationType.SpawnPoint:
                StateMachine.HandleMovement(allowMovement: false, isAirborne: false);
                StateMachine.HandleRotation(allowRotation: false, alignWithCameraWhenIdle: false);
                break;
            case TeleportationType.EndPoint:
                StateMachine.MoveInDirection(Vector3.up, new Vector3(0,0.5f,0));
                StateMachine.HandleRotation(allowRotation: false, alignWithCameraWhenIdle: false);
                break;
        }
        

    }
    
    private void CheckStateTransitions()
    {
        if (_teleportationComplete)
        {
            StateMachine.SwitchState(StateMachine.GroundedState);
        }
    }
    
    
}