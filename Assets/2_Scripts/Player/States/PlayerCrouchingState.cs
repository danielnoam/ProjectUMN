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
        if (StateMachine.inputHandler.JumpInput)
        {
            StateMachine.SwitchState(StateMachine.JumpingState);
            return;
        }
        
        // Interact
        if (StateMachine.inputHandler.InteractInput)
        {
            StateMachine.InteractWith();
            return;
        }
        
        // Grounded
        if (StateMachine.CanStand)
        {
            if (StateMachine.inputHandler.IsCrouchToggle)
            {
                if (StateMachine.inputHandler.CrouchInput)
                {
                    StateMachine.inputHandler.ConsumeCrouchInput();
                    StateMachine.SwitchState(StateMachine.GroundedState);
                    return;
                }
            } else {
                if (!StateMachine.inputHandler.CrouchInput)
                {
                    StateMachine.SwitchState(StateMachine.GroundedState);
                    return;
                }
            }
        }
    }
}