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
                TestManager.Instance.StartTest(0);
            });
            
            quitButton.onClick.AddListener(TestManager.Instance.QuitApplication);
        }
    }
}
