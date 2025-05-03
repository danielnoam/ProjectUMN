
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
        _spawnPoint = null;
    }
    
    public PlayerTeleportingState(PlayerStateMachine stateMachine, ISpawnPoint spawnPoint, float time, TeleportationType type) : base(stateMachine)
    {
        StateMachine.TeleportingState = this;
        _time = time;
        _teleportationType = type;

        if (spawnPoint != null)
        {
            _teleportationDestination = spawnPoint.GetSpawnPosition();
            _teleportationRotation = spawnPoint.GetSpawnRotation();
            _spawnPoint = spawnPoint;
        }
        else
        {
            _teleportationDestination = Vector3.up;
            _teleportationRotation = Quaternion.identity;
            _spawnPoint = null;
        }

    }
    
    private readonly Vector3 _teleportationDestination;
    private readonly Quaternion _teleportationRotation;
    private readonly TeleportationType _teleportationType;
    private readonly ISpawnPoint _spawnPoint;
    private readonly float _time;
    private bool _teleportationComplete;
    private float _teleportationTimer;


    
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
                StateMachine.onPlayerSpawnedFromCheckpoint?.Invoke(_spawnPoint);
                StateMachine.SetCharacterColliderState(true);
                break;
            case TeleportationType.SpawnPoint:
                StateMachine.onPlayerSpawned?.Invoke(_spawnPoint);
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