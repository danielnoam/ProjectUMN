using System;
using UnityEngine;
using UnityEngine.SceneManagement;
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
                player.InMenuState.SelectPage(player.InMenuState.PausePage);
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
                player.InMenuState.ExitMenu();
            });
            
            removeCurrentTestButton.onClick.AddListener(() =>
            {
                TestManager.Instance.RemoveCurrentTest();
                player.InMenuState.ExitMenu();
            });
            
            
            restartSimulationButton.onClick.AddListener(() =>
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });
        }
    }
    
}
