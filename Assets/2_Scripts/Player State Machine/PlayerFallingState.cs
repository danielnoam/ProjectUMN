using UnityEngine;

public class PlayerFallingState : PlayerBaseState
{
    public PlayerFallingState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void EnterState()
    {
        StateMachine.FallTime = 0f;
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
        // Apply movement with air-specific parameters
        StateMachine.HandleMovement(new PlayerStateMachine.MovementParams(
            isAirborne: true,
            speedMultiplier: 1.0f,
            accelMultiplier: 1.0f,
            controlMultiplier: 1.0f
        ));
        
        // Apply gravity with air-specific parameters (false = airborne)
        StateMachine.ApplyGravity(false);
        
        // Apply rotation with air-specific parameters
        StateMachine.HandleRotation(new PlayerStateMachine.RotationParams(
            useAimRotation: StateMachine.IsAiming,
            rotationMultiplier: 0.5f, // Reduced rotation control in air
            allowRotation: true
        ));
    }

    private void CheckStateTransitions()
    {
        // if (StateMachine.IsGrounded)
        // {
        //     if (StateMachine.FallTime > StateMachine.fallThreshold)
        //     {
        //         StateMachine.SwitchState(StateMachine.LandingState);
        //         return;
        //     }
        //     else
        //     {
        //         StateMachine.SwitchState(StateMachine.GroundedState);
        //         return;
        //     }
        // }
        
        if (StateMachine.IsGrounded)
        {
            StateMachine.SwitchState(StateMachine.GroundedState);
        }
    }
}