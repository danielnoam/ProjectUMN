using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TextMeshProUGUI versionTest;
    
    [Header("References")]
    [SerializeField] private PlayerStateMachine player;
    private void Start()
    {
        if (TestManager.Instance)
        {
            startButton.onClick.AddListener(() =>
            {
                player.InMenuState.ExitMenu();
                StartCoroutine(StartGameRoutine());
            });
            
            optionsButton.onClick.AddListener(() =>
            {
                player.InMenuState.SelectPage(player.InMenuState.OptionsPage);
            });
            
            
            creditsButton.onClick.AddListener(() =>
            {
                TestManager.Instance.StartCreditsSequence();
            });
                        
            quitButton.onClick.AddListener(() =>
            {
                player.InMenuState.ExitMenu();
                TestManager.Instance.QuitApplication();
            });
        }
    }
    

    private void OnEnable()
    {
        player?.onPlayerOpenedMenu.AddListener(UpdateVersionText);
    }
    
    private void OnDisable()
    {
        player?.onPlayerOpenedMenu.RemoveListener(UpdateVersionText);
    }
    
    private void UpdateVersionText()
    {
        if (TestManager.Instance)
        {
            versionTest.text = $"V_{TestManager.Instance.PlayerVersion:F4}";
        }
    }


    private IEnumerator StartGameRoutine()
    {
        yield return new WaitForSeconds(2f);
        TestManager.Instance.StartTest(0);
    }
}
