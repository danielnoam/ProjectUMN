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
        StateMachine.HandleAiming();
        StateMachine.CommandRobot();
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.ApplyGravity(true);
    
        StateMachine.HandleMovement(new PlayerStateMachine.MovementParams(
            isAirborne: false,
            speedMultiplier: 1.0f,
            accelMultiplier: 1.0f,
            controlMultiplier: 1.0f,
            dragMultiplier: 1.0f,
            handleSteepSurfaces: false
        ));
    
        StateMachine.HandleRotation(new PlayerStateMachine.RotationParams(
            useAimRotation: StateMachine.IsAiming,
            rotationMultiplier: 0.8f,
            allowRotation: true,
            alignWithCameraWhenIdle: true,
            idleAlignmentSpeed: 1.0f
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