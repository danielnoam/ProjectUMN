using UnityEngine;

public class PlayerLandingState : PlayerBaseState
{
    private float _recoveryProgress;
    private float _landingIntensity;

    public PlayerLandingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        // Calculate landing intensity based on fall time
        _landingIntensity = Mathf.Clamp01(StateMachine.FallTime / StateMachine.maxFallTime);
        
        // Store landing intensity in state machine for animation system to access
        StateMachine.LandingIntensity = _landingIntensity;
        
        // Start recovery at 0
        _recoveryProgress = 0f;
    }
    
    public override void ExitState()
    {
        StateMachine.FallTime = 0f;
        StateMachine.AirTime = 0f;
        
        // Reset landing intensity
        StateMachine.LandingIntensity = 0f;
    }

    public override void UpdateState()
    {
        StateMachine.HandleAiming();
        StateMachine.CommandRobot();
        _recoveryProgress = Mathf.Min(1f, _recoveryProgress + (Time.deltaTime / (StateMachine.recoveryDuration * _landingIntensity)));
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.ApplyGravity(true);
        
        // Calculate movement control based on landing intensity and recovery progress
        float movementControl = Mathf.Lerp(
            StateMachine.minMovementControl,
            1f,
            _recoveryProgress
        );
        
        // Apply movement with landing-specific parameters
        StateMachine.HandleMovement(new PlayerStateMachine.MovementParams(
            isAirborne: false,
            speedMultiplier: 0.8f, // Reduced speed during recovery
            accelMultiplier: 0.7f, // Slower acceleration during recovery
            controlMultiplier: movementControl // Limited control based on recovery
        ));
        
        // Apply rotation with landing-specific parameters
        StateMachine.HandleRotation(new PlayerStateMachine.RotationParams(
            useAimRotation: StateMachine.IsAiming,
            rotationMultiplier: movementControl, // Limited rotation control during recovery
            allowRotation: true
        ));
    }

    private void CheckStateTransitions()
    {
        // Grounded
        if (_recoveryProgress >= 1f)
        {
            StateMachine.SwitchState(StateMachine.GroundedState);
            return;
        }
        
        // Jump
        if (StateMachine.InputHandler.JumpInput)
        {
            StateMachine.SwitchState(StateMachine.JumpingState);
            return;
        }
    }
}