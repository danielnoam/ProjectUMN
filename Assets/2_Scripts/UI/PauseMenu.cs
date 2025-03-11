using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button debugMenuButton;
    [SerializeField] private Button restartTestButton; 
    [SerializeField] private Button quitSimulationButton;

    [Header("References")]
    [SerializeField] private PlayerStateMachine player;
    
    private void Start()
    {
        if (TestManager.Instance)
        {
            debugMenuButton.onClick.AddListener(() =>
            {
                player.InMenuState.SelectPage(player.InMenuState.DebugPage);
            });
            
            restartTestButton.onClick.AddListener(() =>
            {
                TestManager.Instance.RestartCurrentTest(); 
                player.SwitchState(player.GroundedState);
            });
            
            
            quitSimulationButton.onClick.AddListener(TestManager.Instance.QuitApplication);
        }
    }
    
}
