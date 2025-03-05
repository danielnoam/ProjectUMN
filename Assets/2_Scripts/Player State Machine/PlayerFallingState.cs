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
        StateMachine.CommandRobot();
        StateMachine.HandleAiming();
        StateMachine.AirTime += Time.deltaTime;
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.HandleMovement(new PlayerStateMachine.MovementParams(
            isAirborne: true,
            speedMultiplier: 1.0f,
            accelMultiplier: 1.0f,
            controlMultiplier: 1.0f,
            dragMultiplier: 0.5f,
            handleSteepSurfaces: true
        ));
    
        StateMachine.ApplyGravity(false);
    
        StateMachine.HandleRotation(new PlayerStateMachine.RotationParams(
            useAimRotation: StateMachine.IsAiming,
            rotationMultiplier: 0.5f,
            allowRotation: true,
            alignWithCameraWhenIdle: false,
            idleAlignmentSpeed: 0.5f
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