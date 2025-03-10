using UnityEngine;

public enum MenuTypes
{
    Start,
    Pause,
    Debug,
}

public class PlayerInMenuState : PlayerBaseState
{
    public PlayerInMenuState(PlayerStateMachine stateMachine) : base(stateMachine) { }
    public MenuTypes currentMenu { get; set; } = MenuTypes.Pause;
    
    public override void EnterState()
    {
        StateMachine.onPlayerOpenedMenu?.Invoke();
        StateMachine.ClearCurrentInteractable();
        StateMachine.ClearCurrentAimedInteractable();
        StateMachine.InputHandler.ConsumeToggleMenuBuffer();
        StateMachine.menu.SetActive(true);
    }
    
    public override void ExitState()
    {
        StateMachine.InputHandler.ConsumeToggleMenuBuffer();
        StateMachine.menu.SetActive(false);
        ChangeMenu(MenuTypes.Pause);
    }

    public override void UpdateState()
    {
        StateMachine.CheckEnvironmentCollisions();
        StateMachine.HandleAiming(false);
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
            return;
        }

        // Grounded
        if (StateMachine.InputHandler.ToggleMenuInput)
        {
            StateMachine.SwitchState(StateMachine.GroundedState);
            return;
        }
    }

    public void ChangeMenu(MenuTypes menuType)
    {
        if (currentMenu == menuType) return;
        
        currentMenu = menuType;
    }
}