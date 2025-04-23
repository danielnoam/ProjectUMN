using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    
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
            
            quitButton.onClick.AddListener(TestManager.Instance.QuitApplication);
        }
    }
    
    
    
    private IEnumerator StartGameRoutine()
    {
        yield return new WaitForSeconds(2f);
        TestManager.Instance.StartTest(0);
    }
}
