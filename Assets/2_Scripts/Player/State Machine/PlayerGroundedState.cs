using UnityEngine;

public class PlayerGroundedState : PlayerBaseState
{
    public PlayerGroundedState(PlayerStateMachine stateMachine) : base(stateMachine) 
    {
    }
    
    public override void EnterState()
    {
    }
    
    public override void ExitState()
    {
    }

    public override void UpdateState()
    {
        StateMachine.HandleAiming(true);
        StateMachine.CommandRobot();
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.CheckEnvironmentCollisions();
        StateMachine.CheckForInteractable();
        StateMachine.ApplyGravity(true);
        StateMachine.HandleMovement(allowMovement: true, isAirborne: false);
        StateMachine.HandleRotation(allowRotation: true, alignWithCameraWhenIdle: false);
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
        
        // Crouching
        if (StateMachine.InputHandler.IsCrouchToggle)
        {
            if (StateMachine.InputHandler.CrouchInput)
            {
                StateMachine.InputHandler.ConsumeCrouchInput();
                StateMachine.SwitchState(StateMachine.CrouchingState);
                return;
            }
        } else {
            if (StateMachine.InputHandler.CrouchInput)
            {
                StateMachine.SwitchState(StateMachine.CrouchingState);
                return;
            }
        }
        
        // Toggle menu
        if (StateMachine.InputHandler.ToggleMenuInput)
        {
            StateMachine.SwitchState(StateMachine.InMenuState);
            return;
        }
    }
}