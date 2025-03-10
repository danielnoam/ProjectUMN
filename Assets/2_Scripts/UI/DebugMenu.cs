using UnityEngine;
using UnityEngine.UI;

public class DebugMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button toggleDebugMode;
    [SerializeField] private Button nextTestButton;
    [SerializeField] private Button removeCurrentTestButton;
    [SerializeField] private Button restartSimulationButton; 
    [SerializeField] private Button quitButton;

    [Header("References")]
    [SerializeField] private PlayerStateMachine player;
    [SerializeField] private InfoTextHandler infoText;
    
    private void Start()
    {
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
            
            
            quitButton.onClick.AddListener(TestManager.Instance.QuitApplication);
        }
    }
}
