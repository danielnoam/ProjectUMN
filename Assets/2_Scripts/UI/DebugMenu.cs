using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DebugMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button pauseMenuButton;
    [SerializeField] private Button toggleDebugMode;
    [SerializeField] private Button removeCurrentTestButton;
    [SerializeField] private Button restartSimulationButton;
    [SerializeField] private Button loadTest0;
    [SerializeField] private Button loadTest1;
    [SerializeField] private Button loadTest2;
    [SerializeField] private Button loadTest3;

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
            
            removeCurrentTestButton.onClick.AddListener(() =>
            {
                TestManager.Instance.StartClearTestSequence();
                player.InMenuState.ExitMenu();
            });
            
            
            restartSimulationButton.onClick.AddListener(() =>
            {
                TestManager.Instance.StartIntroSequence();
            });
            
            loadTest0.onClick.AddListener(() =>
            {
                TestManager.Instance.StartTest(0);
            });
            
            loadTest1.onClick.AddListener(() =>
            {
                TestManager.Instance.StartTest(1);
            });
            
            loadTest2.onClick.AddListener(() =>
            {
                TestManager.Instance.StartTest(2);
            });
            
            loadTest3.onClick.AddListener(() =>
            {
                TestManager.Instance.StartTest(3);
            });
        }
    }
    
}
