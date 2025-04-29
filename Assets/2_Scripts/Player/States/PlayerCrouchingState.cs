using UnityEngine;

public class PlayerCrouchingState : PlayerBaseState
{
    public PlayerCrouchingState(PlayerStateMachine stateMachine) : base(stateMachine) 
    {
    }
    
    public override void EnterState()
    {
        StateMachine.SetCharacterHeight(true);
    }
    
    public override void ExitState()
    {
        StateMachine.SetCharacterHeight(false);
    }

    public override void UpdateState()
    {
        StateMachine.HandleAiming(allowAiming: true);
        StateMachine.CommandRobot();
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.CheckEnvironmentCollisions();
        StateMachine.CheckForInteractable();
        StateMachine.ApplyGravity(true);
        StateMachine.HandleMovement(allowMovement: true, isAirborne: false);
        StateMachine.HandleRotation(allowRotation: true, alignWithCameraWhenIdle: true);
    }
    
    private void CheckStateTransitions()
    {
        // Fall
        if (!StateMachine.IsGrounded && StateMachine.FallTime > StateMachine.fallThreshold)
        {
            StateMachine.SwitchState(StateMachine.FallingState);
            return;
        }

        // Jump
        if (StateMachine.InputHandler.JumpInput)
        {
            StateMachine.SwitchState(StateMachine.JumpingState);
            return;
        }
        
        // Interact
        if (StateMachine.InputHandler.InteractInput)
        {
            StateMachine.InteractWith();
            return;
        }
        
        // Grounded
        if (StateMachine.CanStand)
        {
            if (StateMachine.InputHandler.IsCrouchToggle)
            {
                if (StateMachine.InputHandler.CrouchInput)
                {
                    StateMachine.InputHandler.ConsumeCrouchInput();
                    StateMachine.SwitchState(StateMachine.GroundedState);
                    return;
                }
            } else {
                if (!StateMachine.InputHandler.CrouchInput)
                {
                    StateMachine.SwitchState(StateMachine.GroundedState);
                    return;
                }
            }
        }
    }
}