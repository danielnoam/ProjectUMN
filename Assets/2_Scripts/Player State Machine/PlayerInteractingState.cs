using UnityEngine;

public class PlayerInteractingState : PlayerBaseState
{
    public PlayerInteractingState(PlayerStateMachine stateMachine) : base(stateMachine) { }
    
    public override void EnterState()
    {
        StateMachine.InputHandler.ConsumeInteractBuffer();
        StateMachine.ClearCurrentInteractable();
        StateMachine.ClearCurrentAimedInteractable();
    }
    
    public override void ExitState()
    {
    }

    public override void UpdateState()
    {
        StateMachine.CheckEnvironmentCollisions();
        StateMachine.HandleAiming(allowAiming: true);
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.ApplyGravity(true);
        StateMachine.HandleMovement(allowMovement: false, isAirborne: false);
        StateMachine.HandleRotation(allowRotation: false, alignWithCameraWhenIdle: false);
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
    
    public void OnInteractionComplete()
    {
        StateMachine.SwitchState(StateMachine.GroundedState);
    }
}