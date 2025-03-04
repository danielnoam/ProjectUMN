using UnityEngine;

public class PlayerLandingState : PlayerBaseState
{
    private float _recoveryProgress;

    public PlayerLandingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        

    }
    
    public override void ExitState()
    {
        StateMachine.FallTime = 0f;
        StateMachine.AirTime = 0f;
        

    }

    public override void UpdateState()
    {
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.ApplyGravity(true);
        
        // Apply movement with landing-specific parameters
        StateMachine.HandleMovement(new PlayerStateMachine.MovementParams(
            isAirborne: false,
            speedMultiplier: 0.8f, // Reduced speed during recovery
            accelMultiplier: 0.7f, // Slower acceleration during recovery
            controlMultiplier: 1 // Limited control based on recovery
        ));
        
        // Apply rotation with landing-specific parameters
        StateMachine.HandleRotation(new PlayerStateMachine.RotationParams(
            useAimRotation: StateMachine.IsAiming,
            rotationMultiplier: 1, // Limited rotation control during recovery
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