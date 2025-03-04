using UnityEngine;

public class PlayerJumpingState : PlayerBaseState
{
    public PlayerJumpingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        StateMachine.InputHandler.ConsumeJumpBuffer();
        StateMachine.ActiveVerticalVelocity = StateMachine.jumpForce;
        StateMachine.AirTime = 0f;
    }
    
    public override void ExitState()
    {
    }

    public override void UpdateState()
    {
        StateMachine.HandleAiming();
        StateMachine.CommandRobot();
        StateMachine.AirTime += Time.deltaTime;
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        // Apply movement with jump-specific parameters
        StateMachine.HandleMovement(new PlayerStateMachine.MovementParams(
            isAirborne: true,
            speedMultiplier: 1.0f,
            accelMultiplier: 1.0f,
            controlMultiplier: 1.0f
        ));
        
        // Apply gravity with air-specific parameters
        StateMachine.ApplyGravity(false);
        
        // Apply rotation with jump-specific parameters
        StateMachine.HandleRotation(new PlayerStateMachine.RotationParams(
            useAimRotation: StateMachine.IsAiming,
            rotationMultiplier: 0.5f, // Similar to falling state
            allowRotation: true
        ));
    }

    private void CheckStateTransitions()
    {
        // Fall
        if (StateMachine.ActiveVerticalVelocity <= 0)
        {
            StateMachine.SwitchState(StateMachine.FallingState);
            return;
        }
    }
}