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
        StateMachine.HandleAiming();
        StateMachine.CommandRobot();
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.ApplyGravity(true);
        
        // Use the new centralized movement system
        StateMachine.HandleMovement(new PlayerStateMachine.MovementParams(
            isAirborne: false,
            speedMultiplier: 1.0f,
            accelMultiplier: 1.0f,
            controlMultiplier: 1.0f
        ));
        
        // Use the new centralized rotation system
        StateMachine.HandleRotation(new PlayerStateMachine.RotationParams(
            useAimRotation: StateMachine.IsAiming,
            rotationMultiplier: 1.0f,
            allowRotation: true
        ));
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
        if (StateMachine.CanInteract && StateMachine.InputHandler.PlayerInteractInput)
        {
            StateMachine.CurrentInteractable.OnInteractionStart(StateMachine.gameObject);
            StateMachine.SwitchState(StateMachine.InteractingState);
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