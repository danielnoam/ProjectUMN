using UnityEngine;

public class PlayerInteractingState : PlayerBaseState
{
    public PlayerInteractingState(PlayerStateMachine stateMachine) : base(stateMachine) { }
    
    public override void EnterState()
    {
        StateMachine.InputHandler.ConsumePlayerInteractBuffer();
    }
    
    public override void ExitState()
    {
    }

    public override void UpdateState()
    {
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.ApplyGravity(true);
        
        // Apply movement with interacting-specific parameters
        // Setting speed multiplier to 0 to gradually stop the player
        StateMachine.ApplyMovement(new PlayerStateMachine.MovementParams(
            isAirborne: false,
            speedMultiplier: 0f, // Target speed of 0
            accelMultiplier: 2f, // Faster deceleration during interaction
            controlMultiplier: 1.0f
        ));
        
        // No rotation while interacting
        StateMachine.ApplyRotation(new PlayerStateMachine.RotationParams(
            useAimRotation: false,
            rotationMultiplier: 0f,
            allowRotation: false // Prevent rotation while interacting
        ));
    }
    
    private void CheckStateTransitions()
    {
        // Fall
        if (!StateMachine.IsGrounded && StateMachine.FallTime > StateMachine.fallThreshold)
        {
            StateMachine.SwitchState(StateMachine.FallingState);
            StateMachine.CurrentInteractable.CancelInteraction();
            return;
        }

        // Jump
        if (StateMachine.InputHandler.JumpInput)
        {
            StateMachine.SwitchState(StateMachine.JumpingState);
            StateMachine.CurrentInteractable.CancelInteraction();
            return;
        }
    }
    
    public void OnInteractionComplete(IInteractable interactable)
    {
        StateMachine.SwitchState(StateMachine.GroundedState);
    }
}