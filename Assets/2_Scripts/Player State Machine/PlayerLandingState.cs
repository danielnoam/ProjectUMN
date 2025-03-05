using UnityEngine;

public class PlayerLandingState : PlayerBaseState
{
    private float _recoveryProgress;

    public PlayerLandingState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void EnterState()
    {
        

    }
    
    public override void ExitState()
    {
        StateMachine.FallTime = 0f;
        StateMachine.AirTime = 0f;
    }

    public override void UpdateState()
    {
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.ApplyGravity(true);
        StateMachine.ApplyGravity(true);
        StateMachine.HandleMovement(allowMovement: true, isAirborne: false);
        StateMachine.HandleRotation(allowRotation: false, alignWithCameraWhenIdle: false);
    }

    private void CheckStateTransitions()
    {
        // Grounded
        if (_recoveryProgress >= 1f)
        {
            StateMachine.SwitchState(StateMachine.GroundedState);
            return;
        }
        
        // Jump
        if (StateMachine.InputHandler.JumpInput)
        {
            StateMachine.SwitchState(StateMachine.JumpingState);
            return;
        }
    }
}