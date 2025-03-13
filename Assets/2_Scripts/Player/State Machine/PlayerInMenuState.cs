using UnityEngine;

public class PlayerInMenuState : PlayerBaseState
{
    public PlayerInMenuState(PlayerStateMachine stateMachine) : base(stateMachine) { }
    
    public MenuController MenuController { get; private set; }
    public MenuPage CurrentPage { get; private set; }
    public MenuPage StartPage { get; private set; }
    public MenuPage PausePage { get; private set; }
    public MenuPage DebugPage { get; private set; }
    
    public override void EnterState()
    {
        StateMachine.onPlayerOpenedMenu?.Invoke();
        StateMachine.ClearCurrentInteractable();
        StateMachine.ClearCurrentAimedInteractable();
        StateMachine.InputHandler.ConsumeToggleMenuBuffer();
        SelectPage(PausePage);
    }
    
    public override void ExitState()
    {
        // When exiting, only deselect the current page
        if (CurrentPage)
        {
            MenuController.DeselectAllPages(true);
            CurrentPage = null;
        }
    }

    public override void UpdateState()
    {
        StateMachine.HandleAiming(false);
        CheckStateTransitions();
    }

    public override void FixedUpdateState()
    {
        StateMachine.CheckEnvironmentCollisions();
        StateMachine.ApplyGravity(true);
        StateMachine.HandleMovement(allowMovement: false, isAirborne: false);
        StateMachine.HandleRotation(allowRotation: false, alignWithCameraWhenIdle: false);
    }
    
    private void CheckStateTransitions()
    {
        // Fall
        if (!StateMachine.IsGrounded && StateMachine.FallTime > StateMachine.fallThreshold)
        {
            ExitMenu();
            return;
        }

        // Grounded
        if (StateMachine.InputHandler.ToggleMenuInput)
        {
            StateMachine.InputHandler.ConsumeToggleMenuBuffer();
            ExitMenu();
            return;
        }
    }

    public void SelectPage(MenuPage page)
    {
        if (CurrentPage == page || !page) return;
        
        CurrentPage = page;
        MenuController.SelectPage(page, true);
    }

    public void SetupPages(MenuController menuController ,MenuPage startPage, MenuPage pausePage, MenuPage debugPage)
    {
        MenuController = menuController;
        StartPage = startPage;
        PausePage = pausePage;
        DebugPage = debugPage;
    }

    public void ExitMenu()
    {
        StateMachine.SwitchState(StateMachine.GroundedState);
    }
}