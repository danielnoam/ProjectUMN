using UnityEngine;

public class PlayerInMenuState : PlayerBaseState
{
    public PlayerInMenuState(PlayerStateMachine stateMachine) : base(stateMachine) { }
    
    public override void EnterState()
    {
        StateMachine.InputHandler.ConsumeToggleMenuBuffer();
        StateMachine.menu.SetActive(true);
    }
    
    public override void ExitState()
    {
        StateMachine.InputHandler.ConsumeToggleMenuBuffer();
        StateMachine.menu.SetActive(false);
    }

    public override void UpdateState()
    {
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.ApplyGravity(true);
        
        // Apply movement with menu-specific parameters
        // Setting speed multiplier to 0 to gradually stop the player
        StateMachine.ApplyMovement(new PlayerStateMachine.MovementParams(
            isAirborne: false,
            speedMultiplier: 0f, // Target speed of 0
            accelMultiplier: 2f, // Faster deceleration in menu
            controlMultiplier: 1.0f
        ));
        
        // No rotation needed in menu
        StateMachine.ApplyRotation(new PlayerStateMachine.RotationParams(
            useAimRotation: false,
            rotationMultiplier: 0f,
            allowRotation: false // Prevent rotation in menu
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

        // Grounded
        if (StateMachine.InputHandler.ToggleMenuInput)
        {
            StateMachine.SwitchState(StateMachine.GroundedState);
            return;
        }
    }
}