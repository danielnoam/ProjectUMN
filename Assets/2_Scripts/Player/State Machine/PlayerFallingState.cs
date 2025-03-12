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
        StateMachine.HandleAiming(true);
        StateMachine.CommandRobot();
        StateMachine.AirTime += Time.deltaTime;
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.CheckEnvironmentCollisions();
        StateMachine.CheckForInteractable();
        StateMachine.ApplyGravity(false);
        StateMachine.HandleMovement(allowMovement: true, isAirborne: true);
        StateMachine.HandleRotation(allowRotation: true, alignWithCameraWhenIdle: false);
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