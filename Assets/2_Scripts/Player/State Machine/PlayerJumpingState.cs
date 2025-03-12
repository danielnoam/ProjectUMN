using UnityEngine;

public class PlayerJumpingState : PlayerBaseState
{
    public PlayerJumpingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        StateMachine.ClearCurrentInteractable();
        StateMachine.InputHandler.ConsumeJumpBuffer();
        StateMachine.ActiveVerticalVelocity = Mathf.Sqrt(StateMachine.jumpForce * 3 * Mathf.Abs(StateMachine.gravity));
        StateMachine.AirTime = 0f;
    }
    
    public override void ExitState()
    {
    }

    public override void UpdateState()
    {

        StateMachine.AirTime += Time.deltaTime;
        StateMachine.HandleAiming(true);
        StateMachine.CommandRobot();
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.CheckEnvironmentCollisions();
        StateMachine.ApplyGravity(false);
        StateMachine.HandleMovement(allowMovement: true, isAirborne: true);
        StateMachine.HandleRotation(allowRotation: true, alignWithCameraWhenIdle: false);
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