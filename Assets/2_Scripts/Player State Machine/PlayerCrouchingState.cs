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
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.ApplyGravity(true);
        
        // Use the centralized movement system with crouch-specific parameters
        // Typically crouching has reduced movement speed
        StateMachine.ApplyMovement(new PlayerStateMachine.MovementParams(
            isAirborne: false,
            speedMultiplier: 1f, // Reduced speed while crouching  0.6f,
            accelMultiplier: 1f, // Potentially slower acceleration 0.8f,
            controlMultiplier: 1.0f
        ));
        
        // Use the centralized rotation system with crouch-specific parameters
        StateMachine.ApplyRotation(new PlayerStateMachine.RotationParams(
            useAimRotation: StateMachine.IsAiming,
            rotationMultiplier: 0.8f, // Slightly slower rotation while crouched 
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