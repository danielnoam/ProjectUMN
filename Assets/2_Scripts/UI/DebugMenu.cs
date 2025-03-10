using UnityEngine;
using UnityEngine.UI;

public class DebugMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button pauseMenuButton;
    [SerializeField] private Button toggleDebugMode;
    [SerializeField] private Button nextTestButton;
    [SerializeField] private Button removeCurrentTestButton;
    [SerializeField] private Button restartSimulationButton; 

    [Header("References")]
    [SerializeField] private PlayerStateMachine player;
    
    private void Start()
    {
        
        if (pauseMenuButton)
        {
            pauseMenuButton.onClick.AddListener(() =>
            {
                player.InMenuState.ChangeMenu(MenuTypes.Pause);
            });
        }
        
        if (TestManager.Instance)
        {
            toggleDebugMode.onClick.AddListener(() =>
            {
                TestManager.Instance.ToggleDebugMode();
            });
            
            
            nextTestButton.onClick.AddListener(() =>
            {
                TestManager.Instance.LoadNextTest();
                player.SwitchState(player.GroundedState);
            });
            
            removeCurrentTestButton.onClick.AddListener(() =>
            {
                TestManager.Instance.RemoveCurrentTest();
                player.SwitchState(player.GroundedState);
            });
            
            
            restartSimulationButton.onClick.AddListener(() =>
            {
                TestManager.Instance.StartTest(0); 
                player.SwitchState(player.GroundedState);
            });
        }
    }
    
}
