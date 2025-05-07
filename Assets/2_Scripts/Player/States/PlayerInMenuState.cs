using UnityEngine;

public class PlayerInMenuState : PlayerBaseState
{
    public PlayerInMenuState(PlayerStateMachine stateMachine) : base(stateMachine) { }
    
    public MenuController MenuController { get; private set; }
    public MenuPage CurrentPage { get; private set; }
    public MenuPage PreviousPage { get; private set; }
    public MenuPage StartPage { get; private set; }
    public MenuPage PausePage { get; private set; }
    public MenuPage DebugPage { get; private set; }
    public MenuPage OptionsPage { get; private set; }
    
    public override void EnterState()
    {
        if (StateMachine.inputHandler.InputReader.CurrentControlScheme == ControlType.Gamepad)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        StateMachine.onPlayerOpenedMenu?.Invoke();
        StateMachine.ClearCurrentInteractable();
        StateMachine.ClearCurrentAimedInteractable();
        StateMachine.inputHandler.ConsumeToggleMenuInput();


        if (StateMachine.TestManager && StateMachine.TestManager.CurrentTest == StateMachine.TestManager.IntroTest)
        {
            SelectPage(StartPage);
        }
        else
        {
            SelectPage(PausePage);
        }

    }
    
    public override void ExitState()
    {
        if (CurrentPage)
        {
            MenuController.DeselectAllPages(true);
            PreviousPage = null;
            CurrentPage = null;
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        if (StateMachine.inputHandler.ToggleMenuInput)
        {
            StateMachine.inputHandler.ConsumeToggleMenuInput();
            ExitMenu();
            return;
        }
    }

    public void SelectPage(MenuPage page)
    {
        if (CurrentPage == page || !page || !page.gameObject.activeSelf) return;
        
        PreviousPage = CurrentPage;
        CurrentPage = page;
        MenuController.SelectPage(page, true);
    }

    public void SetupPages(MenuController menuController ,MenuPage startPage, MenuPage pausePage, MenuPage debugPage, MenuPage optionsPage)
    {
        MenuController = menuController;
        StartPage = startPage;
        PausePage = pausePage;
        DebugPage = debugPage;
        OptionsPage = optionsPage;
    }

    public void ExitMenu()
    {
        StateMachine.inputHandler.ConsumeJumpInput();
        StateMachine.inputHandler.ConsumeCrouchInput();
        StateMachine.SwitchState(StateMachine.GroundedState);
    }
}