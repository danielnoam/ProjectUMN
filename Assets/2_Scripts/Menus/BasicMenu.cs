using UnityEngine;
using UnityEngine.UI;

public class BasicMenu : MonoBehaviour
{
    [SerializeField] private Button nextTestButton;
    [SerializeField] private Button removeCurrentTestButton;
    [SerializeField] private Button restartSimulationButton; 
    [SerializeField] private Button quitButton;
    
    private void Start()
    { 
        if (!TestManager.Instance) return;
        
        nextTestButton.onClick.AddListener(TestManager.Instance.LoadNextTest);
        removeCurrentTestButton.onClick.AddListener(TestManager.Instance.RemoveCurrentTest);
        restartSimulationButton.onClick.AddListener(() => { TestManager.Instance.StartTest(0); });
        quitButton.onClick.AddListener(TestManager.Instance.QuitApplication);
    }
}
