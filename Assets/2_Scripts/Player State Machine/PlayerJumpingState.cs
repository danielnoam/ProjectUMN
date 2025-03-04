using UnityEngine;

public class PlayerJumpingState : PlayerBaseState
{
    public PlayerJumpingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        StateMachine.InputHandler.ConsumeJumpBuffer();
        StateMachine.ActiveVerticalVelocity = Mathf.Sqrt(StateMachine.jumpForce * 3 * Mathf.Abs(StateMachine.gravity));
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
        StateMachine.HandleMovement(new PlayerStateMachine.MovementParams(
            isAirborne: true,
            speedMultiplier: 1.0f,
            accelMultiplier: 1.0f,
            controlMultiplier: 1.0f,
            dragMultiplier: 0.5f,
            handleSteepSurfaces: false
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
        // Fall
        if (StateMachine.ActiveVerticalVelocity <= 0)
        {
            StateMachine.SwitchState(StateMachine.FallingState);
            return;
        }
    }
}